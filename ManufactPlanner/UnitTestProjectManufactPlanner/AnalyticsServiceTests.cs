using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManufactPlanner.Services;
using ManufactPlanner.Models;
using UnitTestProjectManufactPlanner.Helpers;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace UnitTestProjectManufactPlanner.Services
{
    [TestClass]
    public class AnalyticsServiceTests
    {
        private PostgresContext _dbContext;
        private AnalyticsService _analyticsService;

        [TestInitialize]
        public void Setup()
        {
            _dbContext = TestDbHelper.CreateInMemoryDbContext();
            _analyticsService = new AnalyticsService(_dbContext);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _dbContext.Dispose();
        }

        [TestMethod]
        public async Task GetTaskCompletionAnalyticsAsync_WithNoTasks_ShouldReturnZeroes()
        {
            // Arrange
            var startDate = DateTime.Now.AddDays(-30);
            var endDate = DateTime.Now;

            // Act
            var result = await _analyticsService.GetTaskCompletionAnalyticsAsync(startDate, endDate);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result["totalTasks"]);
            Assert.AreEqual(0, result["completedPercent"]);
            Assert.AreEqual(0, result["inProgressPercent"]);
            Assert.AreEqual(0, result["pendingPercent"]);
            Assert.AreEqual(0, result["waitingProductionPercent"]);
            Assert.AreEqual(0, result["otherPercent"]);
        }

        [TestMethod]
        public async Task AnalyticsService_WithEmptyDatabase_ShouldHandleGracefully()
        {
            // Arrange
            var emptyContext = TestDbHelper.CreateInMemoryDbContext("EmptyDatabase");
            var service = new AnalyticsService(emptyContext);
            var startDate = DateTime.Now.AddDays(-30);
            var endDate = DateTime.Now;

            try
            {
                // Act & Assert - все методы должны работать без исключений
                var taskAnalytics = await service.GetTaskCompletionAnalyticsAsync(startDate, endDate);
                Assert.IsNotNull(taskAnalytics);

                var employeeAnalytics = await service.GetEmployeeWorkloadAnalyticsAsync(startDate, endDate);
                Assert.IsNotNull(employeeAnalytics);

                var progressAnalytics = await service.GetTasksProgressOverTimeAsync(startDate, endDate);
                Assert.IsNotNull(progressAnalytics);

                var productionAnalytics = await service.GetProductionAnalyticsAsync(startDate, endDate);
                Assert.IsNotNull(productionAnalytics);

                var keyMetrics = await service.GetKeyMetricsAsync(startDate, endDate);
                Assert.IsNotNull(keyMetrics);
            }
            finally
            {
                emptyContext.Dispose();
            }
        }

        [TestMethod]
        public async Task GetTaskCompletionAnalyticsAsync_WithInvalidDateRange_ShouldReturnZeroes()
        {
            // Arrange - неверный диапазон дат (начальная дата больше конечной)
            var startDate = DateTime.Now;
            var endDate = DateTime.Now.AddDays(-30);

            // Act
            var result = await _analyticsService.GetTaskCompletionAnalyticsAsync(startDate, endDate);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result["totalTasks"]);
        }
    }
}