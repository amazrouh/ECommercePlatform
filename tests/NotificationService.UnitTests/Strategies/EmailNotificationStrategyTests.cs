using Core.Enums;
using Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Configurations;
using NotificationService.Strategies;
using NotificationService.Validators;
using NotificationService.UnitTests.TestHelpers;

namespace NotificationService.UnitTests.Strategies;

public class EmailNotificationStrategyTests
{
    private readonly Mock<ILogger<EmailNotificationStrategy>> _loggerMock;
    private readonly Mock<IOptions<EmailConfig>> _optionsMock;
    private readonly Mock<EmailMessageValidator> _validatorMock;
    private readonly EmailNotificationStrategy _strategy;
    private readonly EmailConfig _config;

    public EmailNotificationStrategyTests()
    {
        _loggerMock = new Mock<ILogger<EmailNotificationStrategy>>();
        _config = new EmailConfig
        {
            SmtpServer = "smtp.example.com",
            Port = 587,
            FromAddress = "sender@example.com",
            Username = "username",
            Password = "password"
        };
        _optionsMock = new Mock<IOptions<EmailConfig>>();
        _optionsMock.Setup(x => x.Value).Returns(_config);
        _validatorMock = new Mock<EmailMessageValidator>();
        _strategy = new EmailNotificationStrategy(_loggerMock.Object, _optionsMock.Object, _validatorMock.Object);
    }

    [Fact]
    public void Type_ReturnsEmail()
    {
        // Act & Assert
        _strategy.Type.Should().Be(NotificationType.Email);
    }

    [Fact]
    public async Task SendAsync_WithValidMessage_ReturnsSuccessResult()
    {
        // Arrange
        var message = TestDataBuilder.CreateValidNotificationMessage();

        // Act
        var result = await _strategy.SendAsync(message);

        // Assert
        result.Success.Should().BeTrue();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending email")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithInvalidEmailAddress_ReturnsFailureResult()
    {
        // Arrange
        var message = TestDataBuilder.CreateValidNotificationMessage(to: "invalid-email");

        // Act
        var result = await _strategy.SendAsync(message);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid email address format");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid email address")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithMissingConfiguration_ReturnsFailureResult()
    {
        // Arrange
        var message = TestDataBuilder.CreateValidNotificationMessage();
        _optionsMock.Setup(x => x.Value).Returns(new EmailConfig());

        // Act
        var result = await _strategy.SendAsync(message);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Email configuration is incomplete");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email configuration is incomplete")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
