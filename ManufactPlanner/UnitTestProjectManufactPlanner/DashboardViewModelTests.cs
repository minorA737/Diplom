using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManufactPlanner.ViewModels;
using ManufactPlanner.Models;
using UnitTestProjectManufactPlanner.Helpers;
using System;
using static ManufactPlanner.ViewModels.TasksViewModel;

namespace UnitTestProjectManufactPlanner.ViewModels
{
    [TestClass]
    public class TasksViewModelTests
    {
        private PostgresContext _dbContext;
        private MainWindowViewModel _mainWindowViewModel;
        private TasksViewModel _viewModel;

        [TestInitialize]
        public void Setup()
        {
            _dbContext = TestDbHelper.CreateInMemoryDbContext();
            _mainWindowViewModel = new MainWindowViewModel();
            _viewModel = new TasksViewModel(_mainWindowViewModel, _dbContext);
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
            Assert.IsNotNull(_viewModel);
            Assert.AreEqual(ViewMode.Table, _viewModel.CurrentViewMode);
            Assert.IsTrue(_viewModel.IsTableViewActive);
            Assert.IsFalse(_viewModel.IsKanbanViewActive);
            Assert.IsFalse(_viewModel.IsCalendarViewActive);
        }

        [TestMethod]
        public void SwitchToTableView_Should_Update_ViewMode()
        {
            // Arrange
            _viewModel.CurrentViewMode = ViewMode.Kanban;

            // Act
            _viewModel.SwitchToTableViewCommand.Execute(null);

            // Assert
            Assert.AreEqual(ViewMode.Table, _viewModel.CurrentViewMode);
            Assert.IsTrue(_viewModel.IsTableViewActive);
            Assert.IsFalse(_viewModel.IsKanbanViewActive);
            Assert.IsFalse(_viewModel.IsCalendarViewActive);
        }

        [TestMethod]
        public void SwitchToKanbanView_Should_Update_ViewMode()
        {
            // Act
            _viewModel.SwitchToKanbanViewCommand.Execute(null);

            // Assert
            Assert.AreEqual(ViewMode.Kanban, _viewModel.CurrentViewMode);
            Assert.IsFalse(_viewModel.IsTableViewActive);
            Assert.IsTrue(_viewModel.IsKanbanViewActive);
            Assert.IsFalse(_viewModel.IsCalendarViewActive);
        }

        

        [TestMethod]
        public void CreateTaskCommand_Should_Not_Be_Null()
        {
            // Assert
            Assert.IsNotNull(_viewModel.CreateTaskCommand);
        }

        [TestMethod]
        public void OpenTaskDetailsCommand_Should_Not_Be_Null()
        {
            // Assert
            Assert.IsNotNull(_viewModel.OpenTaskDetailsCommand);
        }
    }
}