using ManufactPlanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ManufactPlanner.Services
{
    public class DataLensService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly PostgresContext _dbContext;
        private string _iframeUrl;

        // Singleton pattern
        private static DataLensService _instance;
        public static DataLensService Instance => _instance ??= new DataLensService();

        // Конструктор приватный, так как используем Singleton
        private DataLensService()
        {
            _dbContext = new PostgresContext();
            // URL для встраивания дашборда DataLens (нужно заменить на ваш)
            _iframeUrl = "https://datalens.yandex/your-dashboard-id";
        }

        // Получить URL дашборда
        public string GetDashboardUrl() => _iframeUrl;

        // Экспорт данных в DataLens через CSV
        public async System.Threading.Tasks.Task ExportDataToCsv(DateTime startDate, DateTime endDate, string reportType)
        {
            try
            {
                var fileName = $"export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var csvData = GenerateCsvData(startDate, endDate, reportType);

                await UploadToDataLens(csvData, fileName);
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Error exporting data: {ex.Message}");
            }
        }

        // Генерация CSV на основе типа отчета
        private string GenerateCsvData(DateTime startDate, DateTime endDate, string reportType)
        {
            var sb = new StringBuilder();

            // В зависимости от типа отчета формируем заголовки CSV
            switch (reportType)
            {
                case "tasks":
                    sb.AppendLine("TaskId,Name,Status,StartDate,EndDate,AssigneeName");

                    // Получение данных из БД
                    var tasks = _dbContext.TaskDetailsViews
                        .Where(t => t.StartDate >= new DateOnly(startDate.Year, startDate.Month, startDate.Day) &&
                                  (t.EndDate <= new DateOnly(endDate.Year, endDate.Month, endDate.Day) || t.EndDate == null))
                        .ToList();

                    // Формирование строк CSV
                    foreach (var task in tasks)
                    {
                        sb.AppendLine($"{task.Id},{task.Name},{task.Status},{task.StartDate},{task.EndDate},{task.AssigneeName}");
                    }
                    break;

                case "employees":
                    sb.AppendLine("EmployeeId,Name,Department,CompletedTasks,InProgressTasks,TotalTasks");

                    // Логика для отчета по сотрудникам
                    var userIds = _dbContext.Tasks
                        .Where(t => t.StartDate >= new DateOnly(startDate.Year, startDate.Month, startDate.Day))
                        .Select(t => t.AssigneeId)
                        .Distinct()
                        .ToList();

                    foreach (var userId in userIds)
                    {
                        if (userId == null) continue;

                        var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
                        if (user == null) continue;

                        var completedTasks = _dbContext.Tasks.Count(t => t.AssigneeId == userId && t.Status == "Завершено");
                        var inProgressTasks = _dbContext.Tasks.Count(t => t.AssigneeId == userId && t.Status == "В процессе");
                        var totalTasks = _dbContext.Tasks.Count(t => t.AssigneeId == userId);

                        var departmentName = _dbContext.UserDepartments
                            .Where(ud => ud.UserId == userId)
                            .Join(_dbContext.Departments, ud => ud.DepartmentId, d => d.Id, (ud, d) => d.Name)
                            .FirstOrDefault() ?? "Не указан";

                        sb.AppendLine($"{userId},{user.FirstName} {user.LastName},{departmentName},{completedTasks},{inProgressTasks},{totalTasks}");
                    }
                    break;

                case "production":
                    sb.AppendLine("OrderNumber,ProductName,Master,ProductionDate,DebuggingDate,Status");

                    var productionDetails = _dbContext.ProductionDetails
                        .Where(p => p.ProductionDate >= new DateOnly(startDate.Year, startDate.Month, startDate.Day) &&
                               (p.PackagingDate <= new DateOnly(endDate.Year, endDate.Month, endDate.Day) || p.PackagingDate == null))
                        .Join(_dbContext.OrderPositions,
                            p => p.OrderPositionId,
                            op => op.Id,
                            (p, op) => new { p.OrderNumber, op.ProductName, p.MasterName, p.ProductionDate, p.DebuggingDate })
                        .ToList();

                    foreach (var prod in productionDetails)
                    {
                        var status = prod.DebuggingDate.HasValue ? "На отладке" : "В производстве";
                        sb.AppendLine($"{prod.OrderNumber},{prod.ProductName},{prod.MasterName},{prod.ProductionDate},{prod.DebuggingDate},{status}");
                    }
                    break;
            }

            return sb.ToString();
        }

        // Загрузка данных в DataLens (пример - на самом деле требуется API ключ и доступ)
        private async System.Threading.Tasks.Task UploadToDataLens(string csvData, string fileName)
        {
            // Эта реализация зависит от конкретного API DataLens
            // Ниже упрощенный пример для иллюстрации

            try
            {
                var content = new StringContent(csvData, Encoding.UTF8, "text/csv");

                // В реальности здесь должны быть заголовки авторизации и правильный URL API
                var response = await _httpClient.PostAsync($"https://datalens.yandex/api/datasets/upload?name={fileName}", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Data successfully uploaded to DataLens");
                }
                else
                {
                    Console.WriteLine($"Failed to upload data: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading to DataLens: {ex.Message}");
            }
        }

        // Получение аналитических данных для прямого отображения в приложении (без DataLens)
        public Dictionary<string, object> GetAnalyticsData(DateTime startDate, DateTime endDate)
        {
            var result = new Dictionary<string, object>();

            try
            {
                // Общая статистика по задачам
                var tasks = _dbContext.Tasks.Where(t =>
                    (t.StartDate >= new DateOnly(startDate.Year, startDate.Month, startDate.Day) || t.StartDate == null) &&
                    (t.EndDate <= new DateOnly(endDate.Year, endDate.Month, endDate.Day) || t.EndDate == null))
                    .ToList();

                var tasksTotal = tasks.Count;
                var tasksCompleted = tasks.Count(t => t.Status == "Завершено" || t.Status == "Готово");
                var tasksInProgress = tasks.Count(t => t.Status == "В процессе" || t.Status == "В работе");
                var tasksPending = tasks.Count(t => t.Status == "В очереди" || t.Status == "Ожидание");
                var tasksOverdue = tasks.Count(t => t.EndDate < new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day) &&
                                             (t.Status != "Завершено" && t.Status != "Готово"));

                result["tasksTotal"] = tasksTotal;
                result["tasksCompleted"] = tasksCompleted;
                result["tasksInProgress"] = tasksInProgress;
                result["tasksPending"] = tasksPending;
                result["tasksOverdue"] = tasksOverdue;

                // Распределение по статусам в процентах
                if (tasksTotal > 0)
                {
                    result["completedPercent"] = Math.Round((double)tasksCompleted / tasksTotal * 100);
                    result["inProgressPercent"] = Math.Round((double)tasksInProgress / tasksTotal * 100);
                    result["pendingPercent"] = Math.Round((double)tasksPending / tasksTotal * 100);
                    result["overduePercent"] = Math.Round((double)tasksOverdue / tasksTotal * 100);
                    result["otherPercent"] = 100 - (double)result["completedPercent"] -
                                              (double)result["inProgressPercent"] -
                                              (double)result["pendingPercent"] -
                                              (double)result["overduePercent"];
                }

                // Загрузка сотрудников
                var employees = _dbContext.Users
                    .Where(u => _dbContext.Tasks.Any(t => t.AssigneeId == u.Id))
                    .Take(5)  // Топ-5 сотрудников по количеству задач
                    .ToList();

                var employeeData = new List<Dictionary<string, object>>();
                foreach (var emp in employees)
                {
                    var assignedTasks = _dbContext.Tasks.Count(t => t.AssigneeId == emp.Id);
                    var completedTasks = _dbContext.Tasks.Count(t => t.AssigneeId == emp.Id &&
                                                           (t.Status == "Завершено" || t.Status == "Готово"));

                    // Расчет загрузки (пример)
                    var loadPercent = assignedTasks > 0 ?
                        (int)Math.Min(100, Math.Round((double)assignedTasks / 10 * 100)) : 0; // 10 задач = 100% загрузка

                    employeeData.Add(new Dictionary<string, object>
                    {
                        ["name"] = $"{emp.FirstName} {emp.LastName}",
                        ["loadPercent"] = loadPercent,
                        ["tasksTotal"] = assignedTasks,
                        ["tasksCompleted"] = completedTasks
                    });
                }

                result["employeeData"] = employeeData;

                // Метрики эффективности
                var avgTaskDuration = 0.0;
                var completedTasksWithDates = tasks.Where(t =>
                    t.Status == "Завершено" && t.StartDate.HasValue && t.EndDate.HasValue).ToList();

                if (completedTasksWithDates.Any())
                {
                    var totalDays = completedTasksWithDates.Sum(t =>
                        ((DateOnly)t.EndDate).DayNumber - ((DateOnly)t.StartDate).DayNumber);
                    avgTaskDuration = Math.Round((double)totalDays / completedTasksWithDates.Count, 1);
                }

                result["avgTaskDuration"] = avgTaskDuration;

                // Процент задач, выполненных в срок
                var tasksWithDeadlines = tasks.Where(t =>
                    t.Status == "Завершено" && t.EndDate.HasValue).Count();

                var tasksCompletedOnTime = tasks.Count(t =>
                    t.Status == "Завершено" && t.EndDate.HasValue &&
                    t.EndDate >= new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day));

                result["onTimeCompletionRate"] = tasksWithDeadlines > 0 ?
                    Math.Round((double)tasksCompletedOnTime / tasksWithDeadlines * 100) : 0;

                // Данные для временного графика (упрощенно)
                var months = new[] { "Янв", "Фев", "Мар", "Апр", "Май", "Июн" };
                var plannedTasksData = new int[] { 10, 12, 15, 18, 20, 22 };  // Пример данных
                var completedTasksData = new int[] { 8, 10, 13, 15, 18, 20 };  // Пример данных

                result["timeChartMonths"] = months;
                result["timeChartPlannedData"] = plannedTasksData;
                result["timeChartCompletedData"] = completedTasksData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting analytics data: {ex.Message}");
                // Добавляем ошибку в результат
                result["error"] = ex.Message;
            }

            return result;
        }
    }
}