using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManufactPlanner.ViewModels;
using ManufactPlanner.Models;
using UnitTestProjectManufactPlanner.Helpers;
using System;
using System.Threading.Tasks;
using System.Linq;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace UnitTestProjectManufactPlanner.ViewModels
{
    [TestClass]
    public class AuthViewModelTests
    {
        private PostgresContext _dbContext;
        private AuthViewModel _authViewModel;
        private MainWindowViewModel _mockMainWindowViewModel;

        [TestInitialize]
        public void Setup()
        {
            _dbContext = TestDbHelper.CreateInMemoryDbContext();
            _mockMainWindowViewModel = new MainWindowViewModel();
            _authViewModel = new AuthViewModel(_mockMainWindowViewModel, _dbContext);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _dbContext?.Dispose();
        }

        [TestMethod]
        public void Constructor_Should_Initialize_Properties()
        {
            // Assert
            Assert.IsNotNull(_authViewModel);
            Assert.AreEqual(string.Empty, _authViewModel.Username);
            Assert.AreEqual(string.Empty, _authViewModel.Password);
            Assert.IsFalse(_authViewModel.RememberMe);
            Assert.IsFalse(_authViewModel.IsLoading);
            Assert.IsFalse(_authViewModel.HasError);
        }

        [TestMethod]
        public void Username_Property_Should_Update_HasUsername()
        {
            // Act
            _authViewModel.Username = "testuser";

            // Assert
            Assert.AreEqual("testuser", _authViewModel.Username);
            Assert.IsTrue(_authViewModel.HasUsername);
        }

        [TestMethod]
        public void Password_Property_Should_Update_HasPassword()
        {
            // Act
            _authViewModel.Password = "testpass";

            // Assert
            Assert.AreEqual("testpass", _authViewModel.Password);
            Assert.IsTrue(_authViewModel.HasPassword);
        }

        [TestMethod]
        public void ErrorMessage_Property_Should_Update_HasError()
        {
            // Act
            _authViewModel.ErrorMessage = "Test error";

            // Assert
            Assert.AreEqual("Test error", _authViewModel.ErrorMessage);
            Assert.IsTrue(_authViewModel.HasError);
        }

        
        [TestMethod]
        public async Task Login_With_Valid_Credentials_Should_Succeed()
        {
            // Arrange
            // Создаем тестового пользователя
            var testUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Password = "testpass",
                FirstName = "Test",
                LastName = "User",
                IsActive = true
            };

            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            _authViewModel.Username = "testuser";
            _authViewModel.Password = "testpass";

            // Act
            _authViewModel.LoginCommand.Execute();

            // Assert
            Assert.AreEqual(string.Empty, _authViewModel.ErrorMessage);
            Assert.IsFalse(_authViewModel.HasError);
        }

    }
}