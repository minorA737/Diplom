using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManufactPlanner.ViewModels;
using ManufactPlanner.Models;
using ManufactPlanner.Views;
using UnitTestProjectManufactPlanner.Helpers;
using System;

namespace UnitTestProjectManufactPlanner.ViewModels
{
    [TestClass]
    public class MainWindowViewModelTests
    {
        private PostgresContext _dbContext;
        private MainWindowViewModel _viewModel;

        [TestInitialize]
        public void Setup()
        {
            _dbContext = TestDbHelper.CreateInMemoryDbContext();
            _viewModel = new MainWindowViewModel();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _dbContext?.Dispose();
        }


        [TestMethod]
        public void NavigateToDashboard_Should_Update_CurrentMenuItem_And_View()
        {
            // Act
            _viewModel.NavigateToDashboard();

            // Assert
            Assert.AreEqual("dashboard", _viewModel.CurrentMenuItem);
            Assert.IsInstanceOfType(_viewModel.CurrentView, typeof(DashboardPage));
        }

        [TestMethod]
        public void NavigateToOrders_Should_Update_CurrentMenuItem_And_View()
        {
            // Act
            _viewModel.NavigateToOrders();

            // Assert
            Assert.AreEqual("orders", _viewModel.CurrentMenuItem);
            Assert.IsInstanceOfType(_viewModel.CurrentView, typeof(OrdersPage));
        }

        [TestMethod]
        public void NavigateToTasks_Should_Update_CurrentMenuItem_And_View()
        {
            // Act
            _viewModel.NavigateToTasks();

            // Assert
            Assert.AreEqual("tasks", _viewModel.CurrentMenuItem);
            Assert.IsInstanceOfType(_viewModel.CurrentView, typeof(TasksPage));
        }

        [TestMethod]
        public void NavigateToCalendar_Should_Update_CurrentMenuItem_And_View()
        {
            // Act
            _viewModel.NavigateToCalendar();

            // Assert
            Assert.AreEqual("calendar", _viewModel.CurrentMenuItem);
            Assert.IsInstanceOfType(_viewModel.CurrentView, typeof(CalendarPage));
        }

        [TestMethod]
        public void NavigateToTaskDetails_Should_Update_View()
        {
            // Arrange
            int taskId = 123;

            // Act
            _viewModel.NavigateToTaskDetails(taskId);

            // Assert
            Assert.AreEqual("tasks", _viewModel.CurrentMenuItem);
            Assert.IsInstanceOfType(_viewModel.CurrentView, typeof(TaskDetailsPage));
        }

        [TestMethod]
        public void ToggleSidebar_Should_Change_IsSidebarCollapsed()
        {
            // Arrange
            bool initialState = _viewModel.IsSidebarCollapsed;

            // Act
            _viewModel.ToggleSidebar();

            // Assert
            Assert.AreEqual(!initialState, _viewModel.IsSidebarCollapsed);
        }

        [TestMethod]
        public void CurrentUserName_Property_Should_Update_Correctly()
        {
            // Arrange
            string expectedName = "Test User";

            // Act
            _viewModel.CurrentUserName = expectedName;

            // Assert
            Assert.AreEqual(expectedName, _viewModel.CurrentUserName);
        }

        [TestMethod]
        public void UnreadNotificationsCount_Property_Should_Update_Correctly()
        {
            // Arrange
            int expectedCount = 5;

            // Act
            _viewModel.UnreadNotificationsCount = expectedCount;

            // Assert
            Assert.AreEqual(expectedCount, _viewModel.UnreadNotificationsCount);
        }
    }
}