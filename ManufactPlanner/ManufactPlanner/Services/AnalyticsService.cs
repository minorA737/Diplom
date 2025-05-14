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

        /// <summary>
        /// Получить данные о выполнении задач за период
        /// </summary>
        public async Task<Dictionary<string, object>> GetTaskCompletionAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            Console.WriteLine($"GetTaskCompletionAnalyticsAsync: {startDate} - {endDate}");

            var totalTasks = await _dbContext.Tasks
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .CountAsync();

            Console.WriteLine($"Total tasks found: {totalTasks}");

            if (totalTasks == 0)
            {
                return new Dictionary<string, object>
                {
                    ["completedPercent"] = 0,
                    ["inProgressPercent"] = 0,
                    ["pendingPercent"] = 0,
                    ["waitingProductionPercent"] = 0,
                    ["otherPercent"] = 0,
                    ["totalTasks"] = 0
                };
            }

            var completedTasks = await _dbContext.Tasks
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.Status == "Готово")
                .CountAsync();

            var inProgressTasks = await _dbContext.Tasks
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.Status == "В процессе")
                .CountAsync();

            var pendingTasks = await _dbContext.Tasks
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.Status == "В очереди")
                .CountAsync();

            var waitingProductionTasks = await _dbContext.Tasks
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.Status == "Ждем производство")
                .CountAsync();

            var otherTasks = totalTasks - completedTasks - inProgressTasks - pendingTasks - waitingProductionTasks;

            var result = new Dictionary<string, object>
            {
                ["completedPercent"] = Math.Round((double)completedTasks / totalTasks * 100, 1),
                ["inProgressPercent"] = Math.Round((double)inProgressTasks / totalTasks * 100, 1),
                ["pendingPercent"] = Math.Round((double)pendingTasks / totalTasks * 100, 1),
                ["waitingProductionPercent"] = Math.Round((double)waitingProductionTasks / totalTasks * 100, 1),
                ["otherPercent"] = Math.Round((double)otherTasks / totalTasks * 100, 1),
                ["totalTasks"] = totalTasks
            };

            return result;
        }

        /// <summary>
        /// Получить данные о загрузке сотрудников
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetEmployeeWorkloadAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            // Получаем всех пользователей из БД с их задачами
            var employeeWorkload = await _dbContext.Users
                .Where(u => u.IsActive == true)
                .Select(u => new
                {
                    UserId = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    TotalTasks = u.TaskAssignees.Count(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate),
                    CompletedTasks = u.TaskAssignees.Count(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.Status == "Готово"),
                    InProgressTasks = u.TaskAssignees.Count(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.Status == "В процессе"),
                    PendingTasks = u.TaskAssignees.Count(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.Status == "В очереди")
                })
                .OrderByDescending(e => e.TotalTasks)
                .ToListAsync();

            return employeeWorkload.Select(e => new Dictionary<string, object>
            {
                ["name"] = e.FullName,
                ["totalTasks"] = e.TotalTasks,
                ["completedTasks"] = e.CompletedTasks,
                ["inProgressTasks"] = e.InProgressTasks,
                ["pendingTasks"] = e.PendingTasks,
                ["loadPercent"] = e.TotalTasks > 0 ? Math.Round((double)(e.CompletedTasks + e.InProgressTasks) / e.TotalTasks * 100, 0) : 0
            }).ToList();
        }

        /// <summary>
        /// Получить данные о выполнении задач по времени
        /// </summary>
        public async Task<Dictionary<string, object>> GetTasksProgressOverTimeAsync(DateTime startDate, DateTime endDate)
        {
            var monthlyData = new Dictionary<string, Dictionary<string, int>>();

            var current = new DateTime(startDate.Year, startDate.Month, 1);

            while (current <= endDate)
            {
                var monthStart = current;
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var monthName = monthStart.ToString("MMM");

                // Количество задач, завершенных в этом месяце
                var completedCount = await _dbContext.Tasks
                    .Where(t => t.UpdatedAt >= monthStart && t.UpdatedAt <= monthEnd && t.Status == "Готово")
                    .CountAsync();

                // Общее количество задач, созданных в этом месяце
                var plannedCount = await _dbContext.Tasks
                    .Where(t => t.CreatedAt >= monthStart && t.CreatedAt <= monthEnd)
                    .CountAsync();

                monthlyData[monthName] = new Dictionary<string, int>
                {
                    ["completed"] = completedCount,
                    ["planned"] = plannedCount
                };

                current = current.AddMonths(1);
            }

            return new Dictionary<string, object>
            {
                ["monthlyData"] = monthlyData
            };
        }

        /// <summary>
        /// Получить данные о производстве
        /// </summary>
        public async Task<Dictionary<string, object>> GetProductionAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            var productionStats = await _dbContext.ProductionDetails
                .Include(pd => pd.OrderPosition)
                .ThenInclude(op => op.Order)
                .Where(pd => pd.ProductionDate >= DateOnly.FromDateTime(startDate) &&
                            pd.ProductionDate <= DateOnly.FromDateTime(endDate))
                .GroupBy(pd => new {
                    Year = pd.ProductionDate.Value.Year,
                    Month = pd.ProductionDate.Value.Month
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    InProduction = g.Count(pd => pd.ProductionDate != null && pd.PackagingDate == null),
                    Debugging = g.Count(pd => pd.DebuggingDate != null && pd.AcceptanceDate == null),
                    ReadyForPackaging = g.Count(pd => pd.AcceptanceDate != null && pd.PackagingDate == null),
                    Completed = g.Count(pd => pd.PackagingDate != null)
                })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            // Считаем общие показатели
            var totalInProduction = await _dbContext.ProductionDetails
                .Where(pd => pd.ProductionDate != null && pd.PackagingDate == null)
                .CountAsync();

            var totalDebugging = await _dbContext.ProductionDetails
                .Where(pd => pd.DebuggingDate != null && pd.AcceptanceDate == null)
                .CountAsync();

            var totalReadyForPackaging = await _dbContext.ProductionDetails
                .Where(pd => pd.AcceptanceDate != null && pd.PackagingDate == null)
                .CountAsync();

            // Добавьте получение временных данных
            var timelineData = await GetProductionTimelineAsync(startDate, endDate);

            var result = new Dictionary<string, object>
            {
                ["productionStats"] = productionStats.Select(p => new Dictionary<string, object>
                {
                    ["month"] = new DateTime(p.Year, p.Month, 1).ToString("MMM yyyy"),
                    ["inProduction"] = p.InProduction,
                    ["debugging"] = p.Debugging,
                    ["readyForPackaging"] = p.ReadyForPackaging,
                    ["completed"] = p.Completed
                }).ToList(),
                ["totalInProduction"] = totalInProduction,
                ["totalDebugging"] = totalDebugging,
                ["totalReadyForPackaging"] = totalReadyForPackaging,
                ["timelineData"] = timelineData["timelineData"] // Добавить временные данные
            };

            return result;
        }

        /// <summary>
        /// Получить ключевые метрики
        /// </summary>
        public async Task<Dictionary<string, object>> GetKeyMetricsAsync(DateTime startDate, DateTime endDate)
        {
            // Среднее время выполнения задачи
            var completedTasks = await _dbContext.Tasks
                .Where(t => t.Status == "Готово" && t.StartDate != null && t.EndDate != null &&
                           t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToListAsync();

            double avgTaskDuration = 0;
            if (completedTasks.Any())
            {
                avgTaskDuration = completedTasks
                    .Where(t => t.StartDate.HasValue && t.EndDate.HasValue)
                    .Average(t => (t.EndDate.Value.ToDateTime(TimeOnly.MinValue) -
                                  t.StartDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays);
            }

            // Процент выполненных в срок
            var totalTasksWithDeadline = await _dbContext.Tasks
                .Where(t => t.EndDate != null && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .CountAsync();

            var onTimeCompletedTasks = await _dbContext.Tasks
                .Where(t => t.Status == "Готово" && t.EndDate != null &&
                           t.UpdatedAt <= t.EndDate.Value.ToDateTime(TimeOnly.MinValue) &&
                           t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .CountAsync();

            double onTimeCompletionRate = totalTasksWithDeadline > 0
                ? Math.Round((double)onTimeCompletedTasks / totalTasksWithDeadline * 100, 0)
                : 0;

            // Эффективность сотрудников (средний процент выполненных задач)
            var employeeEfficiency = await CalculateEmployeeEfficiencyAsync(startDate, endDate);

            return new Dictionary<string, object>
            {
                ["avgTaskDuration"] = Math.Round(avgTaskDuration, 1),
                ["onTimeCompletionRate"] = onTimeCompletionRate,
                ["employeeEfficiencyRate"] = employeeEfficiency
            };
        }

        /// <summary>
        /// Получить общий набор аналитических данных
        /// </summary>
        public async Task<Dictionary<string, object>> GetComprehensiveAnalyticsAsync(DateTime startDate, DateTime endDate, string reportType = "tasks")
        {
            var result = new Dictionary<string, object>();

            switch (reportType.ToLower())
            {
                case "tasks":
                    // Базовые данные о задачах
                    var taskAnalytics = await GetTaskCompletionAnalyticsAsync(startDate, endDate);
                    foreach (var item in taskAnalytics)
                    {
                        result[item.Key] = item.Value;
                    }

                    // Данные о выполнении по времени
                    var progressData = await GetTasksProgressOverTimeAsync(startDate, endDate);
                    result["progressData"] = progressData;

                    // Ключевые метрики
                    var keyMetrics = await GetKeyMetricsAsync(startDate, endDate);
                    foreach (var item in keyMetrics)
                    {
                        result[item.Key] = item.Value;
                    }
                    break;

                case "employees":
                    // Данные о сотрудниках
                    var employeeData = await GetEmployeeWorkloadAnalyticsAsync(startDate, endDate);
                    result["employeeData"] = employeeData;

                    // Ключевые метрики
                    var keyMetricsEmployees = await GetKeyMetricsAsync(startDate, endDate);
                    foreach (var item in keyMetricsEmployees)
                    {
                        result[item.Key] = item.Value;
                    }
                    break;

                case "production":
                    // Данные о производстве
                    var productionData = await GetProductionAnalyticsAsync(startDate, endDate);
                    result["productionData"] = productionData;
                    break;
            }

            return result;
        }

        private async Task<double> CalculateEmployeeEfficiencyAsync(DateTime startDate, DateTime endDate)
        {
            var employeeEfficiencies = await _dbContext.Users
                .Include(u => u.TaskAssignees)
                .Where(u => u.TaskAssignees.Any(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate))
                .Select(u => new
                {
                    UserId = u.Id,
                    TotalTasks = u.TaskAssignees.Count(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate),
                    CompletedOnTime = u.TaskAssignees.Count(t =>
                        t.CreatedAt >= startDate && t.CreatedAt <= endDate &&
                        t.Status == "Готово" &&
                        t.EndDate != null &&
                        t.UpdatedAt <= t.EndDate.Value.ToDateTime(TimeOnly.MinValue))
                })
                .Where(e => e.TotalTasks > 0)
                .ToListAsync();

            if (!employeeEfficiencies.Any())
                return 0;

            var averageEfficiency = employeeEfficiencies
                .Average(e => (double)e.CompletedOnTime / e.TotalTasks * 100);

            return Math.Round(averageEfficiency, 0);
        }
        /// <summary>
        /// Получить данные для временной линии производства
        /// </summary>
        public async Task<Dictionary<string, object>> GetProductionTimelineAsync(DateTime startDate, DateTime endDate)
        {
            var timelineData = await _dbContext.ProductionDetails
                .Include(pd => pd.OrderPosition)
                .ThenInclude(op => op.Order)
                .Where(pd => pd.ProductionDate >= DateOnly.FromDateTime(startDate) &&
                            pd.ProductionDate <= DateOnly.FromDateTime(endDate))
                .OrderBy(pd => pd.ProductionDate)
                .Select(pd => new
                {
                    OrderNumber = pd.OrderNumber,
                    ProductionDate = pd.ProductionDate,
                    DebuggingDate = pd.DebuggingDate,
                    AcceptanceDate = pd.AcceptanceDate,
                    PackagingDate = pd.PackagingDate,
                    ProductName = pd.OrderPosition.ProductName
                })
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["timelineData"] = timelineData.Select(pd => new Dictionary<string, object>
                {
                    ["orderNumber"] = pd.OrderNumber,
                    ["productName"] = pd.ProductName,
                    ["productionDate"] = pd.ProductionDate?.ToDateTime(TimeOnly.MinValue).ToOADate(),
                    ["debuggingDate"] = pd.DebuggingDate?.ToDateTime(TimeOnly.MinValue).ToOADate(),
                    ["acceptanceDate"] = pd.AcceptanceDate?.ToDateTime(TimeOnly.MinValue).ToOADate(),
                    ["packagingDate"] = pd.PackagingDate?.ToDateTime(TimeOnly.MinValue).ToOADate()
                }).ToList()
            };
        }
    }
}