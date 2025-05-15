using ManufactPlanner.Services;
using ManufactPlanner.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
namespace UnitTestProjectManufactPlanner.Services;
[TestClass]
public class TrayServiceTests
{
    private Mock<MainWindowViewModel> _mockMainViewModel;
    private TrayService _trayService;

    [TestInitialize]
    public void Setup()
    {
        _mockMainViewModel = new Mock<MainWindowViewModel>();
        _trayService = new TrayService(_mockMainViewModel.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _trayService?.Dispose();
    }

    [TestMethod]
    public void Constructor_WithMainViewModel_InitializesService()
    {
        // Assert
        Assert.IsNotNull(_trayService);
    }

    [TestMethod]
    public void IsInTray_InitialState_IsFalse()
    {
        // Assert
        Assert.IsFalse(_trayService.IsInTray);
    }

    [TestMethod]
    public void HideFromTray_SetsIsInTrayToFalse()
    {
        // Act
        _trayService.HideFromTray();

        // Assert
        Assert.IsFalse(_trayService.IsInTray);
    }

    

    
}