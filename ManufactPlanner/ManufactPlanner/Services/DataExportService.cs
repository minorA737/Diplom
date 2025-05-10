using ManufactPlanner.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ManufactPlanner.Services
{
    public class DataExportService
    {
        private readonly PostgresContext _dbContext;
        private readonly string _exportFolder;

        public DataExportService(PostgresContext dbContext)
        {
            _dbContext = dbContext;
            _exportFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ManufactPlanner", "DataExports");

            // Создаем папку, если она не существует
            if (!Directory.Exists(_exportFolder))
            {
                Directory.CreateDirectory(_exportFolder);
            }
        }

        /// <summary>
        /// Экспортирует данные о задачах в CSV
        /// </summary>
        public async Task<string> ExportTasksDataAsync(DateTime startDate, DateTime endDate)
        {
            var fileName = $"tasks_data_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_exportFolder, fileName);

            // Получаем данные из БД для экспорта
            var tasks = await _dbContext.Tasks
                .Include(t => t.Assignee)
                .Include(t => t.OrderPosition)
                .ThenInclude(op => op.Order)
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToListAsync();

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Пишем заголовки CSV
                writer.WriteLine("TaskId,Name,Status,Priority,StartDate,EndDate,AssigneeName,OrderNumber,CreatedAt");

                // Пишем данные
                foreach (var task in tasks)
                {
                    writer.WriteLine(
                        $"{task.Id}," +
                        $"\"{task.Name.Replace("\"", "\"\"")}\"," +
                        $"\"{task.Status}\"," +
                        $"{task.Priority}," +
                        $"{task.StartDate?.ToString("yyyy-MM-dd") ?? ""}," +
                        $"{task.EndDate?.ToString("yyyy-MM-dd") ?? ""}," +
                        $"\"{task.Assignee?.FirstName} {task.Assignee?.LastName}\"," +
                        $"\"{task.OrderPosition?.Order?.OrderNumber ?? ""}\"," +
                        $"{task.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}"
                    );
                }
            }

            return filePath;
        }

        /// <summary>
        /// Экспортирует данные о загрузке сотрудников в CSV
        /// </summary>
        public async Task<string> ExportEmployeeWorkloadDataAsync(DateTime startDate, DateTime endDate)
        {
            var fileName = $"employee_workload_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_exportFolder, fileName);

            // Получаем данные о сотрудниках и их задачах
            var users = await _dbContext.Users
                .Include(u => u.TaskAssignees)
                .Where(u => u.UserDepartments.Any()) // Только сотрудники, которые в отделах
                .ToListAsync();

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Пишем заголовки CSV
                writer.WriteLine("EmployeeId,EmployeeName,Department,TotalTasks,CompletedTasks,ActiveTasks,DelayedTasks,EfficiencyPercent");

                // Пишем данные по каждому сотруднику
                foreach (var user in users)
                {
                    var userTasks = user.TaskAssignees
                        .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                        .ToList();

                    var totalTasks = userTasks.Count;
                    var completedTasks = userTasks.Count(t => t.Status == "Готово" || t.Status == "Завершено");
                    var activeTasks = userTasks.Count(t => t.Status == "В работе" || t.Status == "В процессе");
                    var delayedTasks = userTasks.Count(t => t.EndDate < DateOnly.FromDateTime(DateTime.Now) && t.Status != "Готово" && t.Status != "Завершено");

                    // Рассчитываем эффективность (завершенные задачи / всего задач)
                    double efficiency = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0;

                    // Получаем первый отдел сотрудника
                    var department = user.UserDepartments.FirstOrDefault()?.Department?.Name ?? "";

                    writer.WriteLine(
                        $"{user.Id}," +
                        $"\"{user.FirstName} {user.LastName}\"," +
                        $"\"{department}\"," +
                        $"{totalTasks}," +
                        $"{completedTasks}," +
                        $"{activeTasks}," +
                        $"{delayedTasks}," +
                        $"{efficiency:F1}"
                    );
                }
            }

            return filePath;
        }

        /// <summary>
        /// Экспортирует данные о производстве в CSV
        /// </summary>
        public async Task<string> ExportProductionDataAsync(DateTime startDate, DateTime endDate)
        {
            var fileName = $"production_data_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_exportFolder, fileName);

            // Получаем данные о производстве
            var productionDetails = await _dbContext.ProductionDetails
                .Include(p => p.OrderPosition)
                .ThenInclude(op => op.Order)
                .Where(p => p.UpdatedAt >= startDate && p.UpdatedAt <= endDate)
                .ToListAsync();

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Пишем заголовки CSV
                writer.WriteLine("OrderNumber,ProductName,Master,ProductionDate,DebuggingDate,AcceptanceDate,PackagingDate,Status,Duration");

                // Пишем данные по каждому элементу производства
                foreach (var item in productionDetails)
                {
                    // Определяем статус производства
                    string status = "Неизвестен";
                    if (item.PackagingDate.HasValue) status = "Упаковано";
                    else if (item.AcceptanceDate.HasValue) status = "Принято";
                    else if (item.DebuggingDate.HasValue) status = "Отладка";
                    else if (item.ProductionDate.HasValue) status = "В производстве";

                    // Рассчитываем длительность (если возможно)
                    int duration = 0;
                    if (item.ProductionDate.HasValue && item.PackagingDate.HasValue)
                    {
                        duration = (item.PackagingDate.Value.DayNumber - item.ProductionDate.Value.DayNumber);
                    }

                    writer.WriteLine(
                        $"\"{item.OrderNumber}\"," +
                        $"\"{item.OrderPosition?.ProductName.Replace("\"", "\"\"") ?? ""}\"," +
                        $"\"{item.MasterName?.Replace("\"", "\"\"") ?? ""}\"," +
                        $"{item.ProductionDate?.ToString("yyyy-MM-dd") ?? ""}," +
                        $"{item.DebuggingDate?.ToString("yyyy-MM-dd") ?? ""}," +
                        $"{item.AcceptanceDate?.ToString("yyyy-MM-dd") ?? ""}," +
                        $"{item.PackagingDate?.ToString("yyyy-MM-dd") ?? ""}," +
                        $"\"{status}\"," +
                        $"{duration}"
                    );
                }
            }

            return filePath;
        }
    }
}