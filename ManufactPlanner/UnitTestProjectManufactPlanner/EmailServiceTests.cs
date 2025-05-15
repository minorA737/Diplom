using ManufactPlanner.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace UnitTestProjectManufactPlanner.Services;

[TestClass]
public class EmailServiceTests
{
    private EmailService _emailService;

    [TestInitialize]
    public void Setup()
    {
        _emailService = EmailService.Instance;
    }

    [TestMethod]
    public void IsValidEmail_ValidEmail_ReturnsTrue()
    {
        // Arrange
        string validEmail = "test@example.com";

        // Act
        bool result = _emailService.IsValidEmail(validEmail);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidEmail_InvalidEmail_ReturnsFalse()
    {
        // Arrange
        string invalidEmail = "invalid-email";

        // Act
        bool result = _emailService.IsValidEmail(invalidEmail);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsValidEmail_NullOrEmpty_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.IsFalse(_emailService.IsValidEmail(null));
        Assert.IsFalse(_emailService.IsValidEmail(""));
        Assert.IsFalse(_emailService.IsValidEmail("   "));
    }

    [TestMethod]
    public async Task CheckSmtpConnectionAsync_WithValidSettings_ReturnsTrue()
    {
        // Act
        bool result = await _emailService.CheckSmtpConnectionAsync();

        // Assert
        // Примечание: этот тест может завершиться неудачей, если SMTP-сервер недоступен
        // В реальных условиях стоит мокать SMTP-клиент
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task SendNotificationEmailAsync_InvalidEmail_ReturnsFalse()
    {
        // Act
        bool result = await _emailService.SendNotificationEmailAsync("invalid-email", "Test", "Test message");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task SendTestEmailAsync_InvalidEmail_ReturnsFailure()
    {
        // Act
        var result = await _emailService.SendTestEmailAsync("invalid-email");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("некорректный email-адрес"));
    }
}