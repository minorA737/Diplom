using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManufactPlanner.Services;
using ManufactPlanner.Models;
using UnitTestProjectManufactPlanner.Helpers;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Task = System.Threading.Tasks.Task;

namespace UnitTestProjectManufactPlanner.Services
{
    [TestClass]
    public class DataExportServiceTests
    {
        private PostgresContext _dbContext;
        private DataExportService _dataExportService;
        private string _exportTestDir;

        [TestInitialize]
        public void Setup()
        {
            _dbContext = TestDbHelper.CreateInMemoryDbContext();
            _dataExportService = new DataExportService(_dbContext);

            // Создаем временную директорию для тестов экспорта
            _exportTestDir = Path.Combine(Path.GetTempPath(), "ManufactPlanner_Export_Test_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_exportTestDir);

            // Используем рефлексию для изменения папки экспорта
            var field = typeof(DataExportService).GetField("_exportFolder",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_dataExportService, _exportTestDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _dbContext.Dispose();

            // Удаляем временную директорию тестов
            try
            {
                if (Directory.Exists(_exportTestDir))
                {
                    Directory.Delete(_exportTestDir, true);
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки очистки
            }
        }

        [TestMethod]
        public async Task ExportTasksDataAsync_WithoutTasks_ShouldCreateEmptyCsvFile()
        {
            // Arrange
            var startDate = DateTime.Now.AddDays(-30);
            var endDate = DateTime.Now;

            // Act
            var filePath = await _dataExportService.ExportTasksDataAsync(startDate, endDate);

            // Assert
            Assert.IsNotNull(filePath);
            Assert.IsTrue(File.Exists(filePath));

            // Проверяем содержимое файла
            var fileContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            Assert.IsTrue(fileContent.Contains("TaskId,Name,Status,Priority,StartDate,EndDate,AssigneeName,OrderNumber,CreatedAt"));

            // Должна быть только строка заголовка
            var lines = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(1, lines.Length);
        }

        [TestMethod]
        public async Task ExportEmployeeWorkloadDataAsync_WithoutDepartments_ShouldHandleGracefully()
        {
            // Arrange
            // Создаем пользователя без отделов
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Password = "password",
                FirstName = "Test",
                LastName = "User",
                Email = "test@test.com",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var startDate = DateTime.Now.AddDays(-30);
            var endDate = DateTime.Now;

            // Act & Assert - не должно бросать исключение
            var filePath = await _dataExportService.ExportEmployeeWorkloadDataAsync(startDate, endDate);

            Assert.IsNotNull(filePath);
            Assert.IsTrue(File.Exists(filePath));
        }

    }
}