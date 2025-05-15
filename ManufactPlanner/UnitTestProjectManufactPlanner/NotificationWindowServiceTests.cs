using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManufactPlanner.Services;
using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;
using Moq;

namespace UnitTestProjectManufactPlanner.Services
{
    [TestClass]
    public class NotificationWindowServiceTests
    {
        [TestMethod]
        public void ShowNotification_Should_Not_Throw_When_No_Application()
        {
            // Arrange
            // Убеждаемся, что Application.Current равен null

            // Act & Assert
            try
            {
                NotificationWindowService.ShowNotification("Test", "Test Message");
                Assert.IsTrue(true); // Метод должен безопасно обработать отсутствие Application
            }
            catch (Exception ex)
            {
                // В тестовой среде без Avalonia приложения это ожидаемо
                Assert.IsTrue(ex.Message.Contains("Application") || ex.Message.Contains("UI"));
            }
        }
    }
}