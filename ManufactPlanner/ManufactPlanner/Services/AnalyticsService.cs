using ManufactPlanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ManufactPlanner.Services
{
    public class AnalyticsService
    {
        private readonly PostgresContext _dbContext;

        public AnalyticsService(PostgresContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Dictionary<string, object>> GetTaskAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            var result = new Dictionary<string, object>();

            try
            {
                // Конвертируем DateTime в DateOnly для сравнения с полями в БД
                var startDateOnly = DateOnly.FromDateTime(startDate);
                var endDateOnly = DateOnly.FromDateTime(endDate);

                // Получаем все задачи в указанном периоде
                var tasks = await _dbContext.Tasks
                    .Where(t => (t.StartDate >= startDateOnly && t.StartDate <= endDateOnly) ||
                               (t.EndDate >= startDateOnly && t.EndDate <= endDateOnly))
                    .ToListAsync();

                // Рассчитываем статистику
                int totalTasks = tasks.Count;
                if (totalTasks == 0)
                {
                    result["error"] = "Нет данных для выбранного периода";
                    return result;
                }

                // Статусы задач
                int completedCount = tasks.Count(t => t.Status == "Завершено" || t.Status == "Готово");
                int inProgressCount = tasks.Count(t => t.Status == "В процессе" || t.Status == "В работе");
                int pendingCount = tasks.Count(t => t.Status == "В очереди" || t.Status == "Ожидание");
                int overdueCount = tasks.Count(t =>
                    (t.EndDate < DateOnly.FromDateTime(DateTime.Now)) &&
                    (t.Status != "Завершено" && t.Status != "Готово"));
                int otherCount = totalTasks - completedCount - inProgressCount - pendingCount - overdueCount;

                // Проценты для круговой диаграммы
                result["completedPercent"] = Convert.ToInt32((double)completedCount / totalTasks * 100);
                result["inProgressPercent"] = Convert.ToInt32((double)inProgressCount / totalTasks * 100);
                result["pendingPercent"] = Convert.ToInt32((double)pendingCount / totalTasks * 100);
                result["overduePercent"] = Convert.ToInt32((double)overdueCount / totalTasks * 100);
                result["otherPercent"] = Convert.ToInt32((double)otherCount / totalTasks * 100);

                // Среднее время выполнения задач
                var completedTasks = tasks.Where(t => t.Status == "Завершено" || t.Status == "Готово" && t.StartDate.HasValue && t.EndDate.HasValue);
                double avgDays = 0;
                if (completedTasks.Any())
                {
                    avgDays = completedTasks.Average(t => (t.EndDate.Value.ToDateTime(TimeOnly.MinValue) - t.StartDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays);
                    result["avgTaskDuration"] = Math.Round(avgDays, 1);
                }
                else
                {
                    result["avgTaskDuration"] = 0;
                }

                // Процент задач, выполненных в срок
                int onTimeCount = tasks.Count(t =>
                    (t.Status == "Завершено" || t.Status == "Готово") &&
                    t.EndDate.HasValue &&
                    t.EndDate.Value <= DateOnly.FromDateTime(DateTime.Now));
                result["onTimeCompletionRate"] = completedCount > 0 ?
                    Convert.ToInt32((double)onTimeCount / completedCount * 100) : 0;

                // Данные о выполнении задач по месяцам для линейного графика
                var monthlyData = GetMonthlyTaskCompletion(tasks, startDate, endDate);
                result["monthlyData"] = monthlyData;

                // Загрузка сотрудников
                var employeeLoad = await GetEmployeeLoadDataAsync(startDate, endDate);
                result["employeeData"] = employeeLoad;

                return result;
            }
            catch (Exception ex)
            {
                result["error"] = $"Ошибка при получении аналитики: {ex.Message}";
                return result;
            }
        }

        private List<Dictionary<string, object>> GetMonthlyTaskCompletion(List<Models.Task> tasks, DateTime startDate, DateTime endDate)
        {
            var result = new List<Dictionary<string, object>>();

            // Подготавливаем список месяцев в диапазоне
            var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
            while (currentDate <= endDate)
            {
                var monthData = new Dictionary<string, object>
                {
                    ["month"] = currentDate.ToString("MMM"),
                    ["year"] = currentDate.Year,
                    ["completed"] = 0,
                    ["planned"] = 0
                };

                result.Add(monthData);
                currentDate = currentDate.AddMonths(1);
            }

            // Заполняем данными по задачам
            foreach (var task in tasks)
            {
                if (task.EndDate.HasValue)
                {
                    var taskEndDate = task.EndDate.Value.ToDateTime(TimeOnly.MinValue);
                    var monthIndex = ((taskEndDate.Year - startDate.Year) * 12) + taskEndDate.Month - startDate.Month;

                    if (monthIndex >= 0 && monthIndex < result.Count)
                    {
                        if (task.Status == "Завершено" || task.Status == "Готово")
                        {
                            result[monthIndex]["completed"] = (int)result[monthIndex]["completed"] + 1;
                        }
                        result[monthIndex]["planned"] = (int)result[monthIndex]["planned"] + 1;
                    }
                }
            }

            return result;
        }

        public async Task<List<Dictionary<string, object>>> GetEmployeeLoadDataAsync(DateTime startDate, DateTime endDate)
        {
            var result = new List<Dictionary<string, object>>();

            try
            {
                // Получаем данные о задачах с их исполнителями
                var tasks = await _dbContext.Tasks
                    .Include(t => t.Assignee)
                    .Where(t => t.AssigneeId != null &&
                               ((t.StartDate != null && t.StartDate >= DateOnly.FromDateTime(startDate)) ||
                                (t.EndDate != null && t.EndDate <= DateOnly.FromDateTime(endDate))))
                    .ToListAsync();

                // Группируем по исполнителям
                var employeeGroups = tasks
                    .GroupBy(t => new { t.AssigneeId, FullName = $"{t.Assignee?.LastName} {t.Assignee?.FirstName}" })
                    .Where(g => g.Key.AssigneeId != null)
                    .Select(g => new
                    {
                        EmployeeId = g.Key.AssigneeId,
                        EmployeeName = g.Key.FullName,
                        TotalTasks = g.Count(),
                        InProgressTasks = g.Count(t => t.Status == "В процессе" || t.Status == "В работе"),
                        CompletedTasks = g.Count(t => t.Status == "Завершено" || t.Status == "Готово")
                    })
                    .OrderByDescending(e => e.InProgressTasks)
                    .Take(10)
                    .ToList();

                // Рассчитываем загрузку (в процентах)
                foreach (var employee in employeeGroups)
                {
                    // Максимальное количество активных задач, которое считается 100% загрузкой
                    const int maxActiveTasks = 5;

                    int loadPercent = Math.Min(Convert.ToInt32((double)employee.InProgressTasks / maxActiveTasks * 100), 100);

                    result.Add(new Dictionary<string, object>
                    {
                        ["employeeId"] = employee.EmployeeId,
                        ["name"] = employee.EmployeeName,
                        ["totalTasks"] = employee.TotalTasks,
                        ["inProgressTasks"] = employee.InProgressTasks,
                        ["completedTasks"] = employee.CompletedTasks,
                        ["loadPercent"] = loadPercent
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting employee load data: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }
    }
}