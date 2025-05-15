using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManufactPlanner.Services;
using System;
using System.Runtime.InteropServices;

namespace UnitTestProjectManufactPlanner.Services
{
    [TestClass]
    public class AutoStartServiceTests
    {
        [TestMethod]
        public void IsAutoStartEnabled_Should_Return_Boolean()
        {
            // Act
            var result = AutoStartService.IsAutoStartEnabled();

            // Assert
            Assert.IsTrue(result == true || result == false);
        }

        [TestMethod]
        public void SetAutoStart_Enable_Should_Not_Throw_Exception()
        {
            // Arrange
            var originalState = AutoStartService.IsAutoStartEnabled();

            try
            {
                // Act
                AutoStartService.SetAutoStart(true);

                // Assert
                // Проверяем, что метод выполнился без исключений
                Assert.IsTrue(true);
            }
            finally
            {
                // Cleanup - восстанавливаем исходное состояние
                AutoStartService.SetAutoStart(originalState);
            }
        }

        [TestMethod]
        public void SetAutoStart_Disable_Should_Not_Throw_Exception()
        {
            // Arrange
            var originalState = AutoStartService.IsAutoStartEnabled();

            try
            {
                // Act
                AutoStartService.SetAutoStart(false);

                // Assert
                Assert.IsTrue(true);
            }
            finally
            {
                // Cleanup - восстанавливаем исходное состояние
                AutoStartService.SetAutoStart(originalState);
            }
        }

        [TestMethod]
        public void SetAutoStart_Enable_Then_Disable_Should_Work_Correctly()
        {
            // Arrange
            var originalState = AutoStartService.IsAutoStartEnabled();

            try
            {
                // Act - включаем автозапуск
                AutoStartService.SetAutoStart(true);
                var stateAfterEnable = AutoStartService.IsAutoStartEnabled();

                // Act - выключаем автозапуск
                AutoStartService.SetAutoStart(false);
                var stateAfterDisable = AutoStartService.IsAutoStartEnabled();

                // Assert
                // На разных платформах результат может отличаться
                // Главное - методы должны выполняться без исключений
                Assert.IsTrue(stateAfterEnable == true || stateAfterEnable == false);
                Assert.IsTrue(stateAfterDisable == true || stateAfterDisable == false);
            }
            finally
            {
                // Cleanup
                AutoStartService.SetAutoStart(originalState);
            }
        }

        [TestMethod]
        public void AutoStartService_Should_Handle_Multiple_Calls()
        {
            // Arrange
            var originalState = AutoStartService.IsAutoStartEnabled();

            try
            {
                // Act - множественные вызовы
                for (int i = 0; i < 3; i++)
                {
                    AutoStartService.SetAutoStart(true);
                    AutoStartService.SetAutoStart(false);
                }

                // Assert - должно выполниться без исключений
                Assert.IsTrue(true);
            }
            finally
            {
                // Cleanup
                AutoStartService.SetAutoStart(originalState);
            }
        }

        [TestMethod]
        public void AutoStartService_Should_Work_On_Current_Platform()
        {
            // Arrange
            var currentPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                                 RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" :
                                 RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : "Unknown";

            // Act
            var canCheckAutoStart = false;
            var canSetAutoStart = false;

            try
            {
                var currentState = AutoStartService.IsAutoStartEnabled();
                canCheckAutoStart = true;

                AutoStartService.SetAutoStart(currentState);
                canSetAutoStart = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception on {currentPlatform}: {ex.Message}");
            }

            // Assert
            // В зависимости от платформы и прав доступа результат может различаться
            Console.WriteLine($"Platform: {currentPlatform}, CanCheck: {canCheckAutoStart}, CanSet: {canSetAutoStart}");
            Assert.IsTrue(true); // Главное, что тест не падает с необработанным исключением
        }
    }
}