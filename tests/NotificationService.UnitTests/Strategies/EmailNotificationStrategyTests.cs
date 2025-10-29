using Core.Enums;
using Core.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Configurations;
using NotificationService.Strategies;

namespace NotificationService.UnitTests.Strategies;

public class EmailNotificationStrategyTests
{
    private readonly Mock<ILogger<EmailNotificationStrategy>> _loggerMock;
    private readonly Mock<IOptions<EmailConfig>> _optionsMock;
    private readonly EmailNotificationStrategy _strategy;
    private readonly EmailConfig _config;

    public EmailNotificationStrategyTests()
    {
        _loggerMock = new Mock<ILogger<EmailNotificationStrategy>>();
        _config = new EmailConfig
        {
            SmtpServer = "smtp.example.com",
            SmtpPort = 587,
            SenderEmail = "sender@example.com",
            Username = "username",
            Password = "password"
        };
        _optionsMock = new Mock<IOptions<EmailConfig>>();
        _optionsMock.Setup(x => x.Value).Returns(_config);
        _strategy = new EmailNotificationStrategy(_loggerMock.Object, _optionsMock.Object);
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
        result.Should().BeSuccessful();
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
        result.Should().BeFailure()
            .And.HaveError("Invalid email address format");
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
        result.Should().BeFailure()
            .And.HaveError("Email configuration is incomplete");
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
