using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManufactPlanner.Services;
using ManufactPlanner.ViewModels;
using Moq;
using System;

namespace UnitTestProjectManufactPlanner.Services
{
    [TestClass]
    public class NotificationDialogServiceTests
    {
        private Mock<MainWindowViewModel> _mockMainViewModel;
        private Mock<NotificationService> _mockNotificationService;

        [TestInitialize]
        public void Setup()
        {
            _mockMainViewModel = new Mock<MainWindowViewModel>();
            _mockNotificationService = new Mock<NotificationService>();
        }

        
        [TestMethod]
        public void CloseAllDialogs_Should_Not_Throw_Exception()
        {
            // Act & Assert
            try
            {
                NotificationDialogService.CloseAllDialogs();
                Assert.IsTrue(true);
            }
            catch (Exception)
            {
                Assert.Fail("CloseAllDialogs should not throw exceptions");
            }
        }
    }
}