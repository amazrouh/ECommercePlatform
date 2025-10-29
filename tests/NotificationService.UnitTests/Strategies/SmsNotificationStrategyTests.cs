using Core.Enums;
using Core.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Configurations;
using NotificationService.Strategies;

namespace NotificationService.UnitTests.Strategies;

public class SmsNotificationStrategyTests
{
    private readonly Mock<ILogger<SmsNotificationStrategy>> _loggerMock;
    private readonly Mock<IOptions<SmsConfig>> _optionsMock;
    private readonly SmsNotificationStrategy _strategy;
    private readonly SmsConfig _config;

    public SmsNotificationStrategyTests()
    {
        _loggerMock = new Mock<ILogger<SmsNotificationStrategy>>();
        _config = new SmsConfig
        {
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret",
            SenderId = "TEST"
        };
        _optionsMock = new Mock<IOptions<SmsConfig>>();
        _optionsMock.Setup(x => x.Value).Returns(_config);
        _strategy = new SmsNotificationStrategy(_loggerMock.Object, _optionsMock.Object);
    }

    [Fact]
    public void Type_ReturnsSms()
    {
        // Act & Assert
        _strategy.Type.Should().Be(NotificationType.Sms);
    }

    [Fact]
    public async Task SendAsync_WithValidMessage_ReturnsSuccessResult()
    {
        // Arrange
        var message = TestDataBuilder.CreateValidNotificationMessage(
            to: "+1234567890",
            body: "Test SMS message");

        // Act
        var result = await _strategy.SendAsync(message);

        // Assert
        result.Should().BeSuccessful();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending SMS")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("not-a-phone-number")]
    [InlineData("123")]
    [InlineData("+")]
    public async Task SendAsync_WithInvalidPhoneNumber_ReturnsFailureResult(string phoneNumber)
    {
        // Arrange
        var message = TestDataBuilder.CreateValidNotificationMessage(to: phoneNumber);

        // Act
        var result = await _strategy.SendAsync(message);

        // Assert
        result.Should().BeFailure()
            .And.HaveError("Invalid phone number format");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid phone number")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithMissingConfiguration_ReturnsFailureResult()
    {
        // Arrange
        var message = TestDataBuilder.CreateValidNotificationMessage(to: "+1234567890");
        _optionsMock.Setup(x => x.Value).Returns(new SmsConfig());

        // Act
        var result = await _strategy.SendAsync(message);

        // Assert
        result.Should().BeFailure()
            .And.HaveError("SMS configuration is incomplete");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SMS configuration is incomplete")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithMessageTooLong_ReturnsFailureResult()
    {
        // Arrange
        var message = TestDataBuilder.CreateValidNotificationMessage(
            to: "+1234567890",
            body: new string('x', 1601)); // SMS typically limited to 1600 chars

        // Act
        var result = await _strategy.SendAsync(message);

        // Assert
        result.Should().BeFailure()
            .And.HaveError("Message exceeds maximum length of 1600 characters");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message exceeds maximum length")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
