using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManufactPlanner.Services;
using ManufactPlanner.Models;
using UnitTestProjectManufactPlanner.Helpers;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace UnitTestProjectManufactPlanner.Services
{
    [TestClass]
    public class UserSettingsServiceTests
    {
        private PostgresContext _dbContext;
        private UserSettingsService _userSettingsService;

        [TestInitialize]
        public void Setup()
        {
            _dbContext = TestDbHelper.CreateInMemoryDbContext();
            _userSettingsService = new UserSettingsService(_dbContext);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _dbContext.Dispose();
        }

        

        [TestMethod]
        public async System.Threading.Tasks.Task GetUserProfileAsync_With_NonExistent_User_Should_Return_Null()
        {
            // Arrange
            var nonExistentUserId = Guid.NewGuid();

            // Act
            var result = await _userSettingsService.GetUserProfileAsync(nonExistentUserId);

            // Assert
            Assert.IsNull(result);
        }

        

        [TestMethod]
        public async System.Threading.Tasks.Task UpdateUserProfileAsync_With_NonExistent_User_Should_Return_False()
        {
            // Arrange
            var nonExistentUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "User",
                Email = "test@email.com"
            };

            // Act
            var result = await _userSettingsService.UpdateUserProfileAsync(nonExistentUser);

            // Assert
            Assert.IsFalse(result);
        }

        
        [TestMethod]
        public async System.Threading.Tasks.Task ChangePasswordAsync_With_NonExistent_User_Should_Return_False()
        {
            // Arrange
            var nonExistentUserId = Guid.NewGuid();
            var currentPassword = "any_password";
            var newPassword = "new_password_123";

            // Act
            var result = await _userSettingsService.ChangePasswordAsync(nonExistentUserId, currentPassword, newPassword);

            // Assert
            Assert.IsFalse(result);
        }

    }
}