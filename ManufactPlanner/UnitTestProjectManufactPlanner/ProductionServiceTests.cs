using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManufactPlanner.Services;
using ManufactPlanner.Models;
using UnitTestProjectManufactPlanner.Helpers;
using System;
using System.Threading.Tasks;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace UnitTestProjectManufactPlanner.Services
{
    [TestClass]
    public class ProductionServiceTests
    {
        private PostgresContext _dbContext;
        private ProductionService _productionService;

        [TestInitialize]
        public void Setup()
        {
            _dbContext = TestDbHelper.CreateInMemoryDbContext();
            _productionService = new ProductionService(_dbContext);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _dbContext.Dispose();
        }

        
        [TestMethod]
        public async Task GetAllProductionOrdersAsync_Should_Return_Orders_Sorted_By_UpdatedAt()
        {
            // Arrange
            var order1 = new ProductionDetail
            {
                OrderPositionId = 1,
                OrderNumber = "PD-001",
                MasterName = "Master 1",
                UpdatedAt = DateTime.Now.AddDays(-2)
            };

            var order2 = new ProductionDetail
            {
                OrderPositionId = 1,
                OrderNumber = "PD-002",
                MasterName = "Master 2",
                UpdatedAt = DateTime.Now.AddDays(-1)
            };

            _dbContext.ProductionDetails.AddRange(order1, order2);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _productionService.GetAllProductionOrdersAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("PD-002", result.First().OrderNumber);
        }

        
    }
}
