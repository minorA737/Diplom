using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManufactPlanner.Services;
using ManufactPlanner.Models;
using UnitTestProjectManufactPlanner.Helpers;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace UnitTestProjectManufactPlanner.Services
{
    [TestClass]
    public class RoleServiceTests
    {
        private PostgresContext _dbContext;
        private RoleService _roleService;

        [TestInitialize]
        public void Setup()
        {
            _dbContext = TestDbHelper.CreateInMemoryDbContext();
            _roleService = RoleService.Instance;

            // Очищаем кэш перед каждым тестом
            _roleService.ClearCache();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _roleService.ClearCache();
            _dbContext.Dispose();
        }

        

        [TestMethod]
        public async System.Threading.Tasks.Task GetUserRolesAsync_With_NonExistent_User_Should_Return_Empty_List()
        {
            // Arrange
            var nonExistentUserId = Guid.NewGuid();

            // Act
            var roles = await _roleService.GetUserRolesAsync(_dbContext, nonExistentUserId);

            // Assert
            Assert.IsNotNull(roles);
            Assert.AreEqual(0, roles.Count);
        }

       

        [TestMethod]
        public void HasRole_Should_Return_False_For_User_Not_In_Cache()
        {
            // Arrange
            var nonExistentUserId = Guid.NewGuid();

            // Act
            var hasRole = _roleService.HasRole(nonExistentUserId, RoleService.ROLE_ADMINISTRATOR);

            // Assert
            Assert.IsFalse(hasRole);
        }

       

        [TestMethod]
        public void RoleService_Should_Be_Singleton()
        {
            // Act
            var instance1 = RoleService.Instance;
            var instance2 = RoleService.Instance;

            // Assert
            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        public void Role_Constants_Should_Be_Defined()
        {
            // Assert
            Assert.IsNotNull(RoleService.ROLE_ADMINISTRATOR);
            Assert.IsNotNull(RoleService.ROLE_MANAGER);
            Assert.IsNotNull(RoleService.ROLE_EXECUTOR);

            Assert.AreEqual("Администратор", RoleService.ROLE_ADMINISTRATOR);
            Assert.AreEqual("Менеджер", RoleService.ROLE_MANAGER);
            Assert.AreEqual("Исполнитель", RoleService.ROLE_EXECUTOR);
        }

        
    }
}