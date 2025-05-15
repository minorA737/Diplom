using ManufactPlanner.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading;
using System;
using System.Reactive.Linq;

namespace UnitTestProjectManufactPlanner.Services;

[TestClass]
public class ThemeServiceTests
{
    private ThemeService _themeService;

    [TestInitialize]
    public void Setup()
    {
        // Создаем новый экземпляр для каждого теста
        // Используем рефлексию для создания нового экземпляра, так как это синглтон
        var instanceField = typeof(ThemeService).GetField("_instance",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        instanceField?.SetValue(null, null);

        _themeService = ThemeService.Instance;
    }

    [TestMethod]
    public void IsLightTheme_DefaultValue_IsTrue()
    {
        // Assert
        Assert.IsTrue(_themeService.IsLightTheme);
    }

    

    

    [TestMethod]
    public void SaveThemeSettings_CreatesFile()
    {
        // Arrange
        const string settingsFile = "app_settings.json";
        if (File.Exists(settingsFile))
            File.Delete(settingsFile);

        // Act
        _themeService.SaveThemeSettings();

        // Assert
        Assert.IsTrue(File.Exists(settingsFile));

        // Cleanup
        if (File.Exists(settingsFile))
            File.Delete(settingsFile);
    }

}