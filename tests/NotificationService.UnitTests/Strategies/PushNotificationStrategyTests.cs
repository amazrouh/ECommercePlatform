using Core.Enums;
using Core.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Configurations;
using NotificationService.Strategies;

namespace NotificationService.UnitTests.Strategies;

public class PushNotificationStrategyTests
{
    private readonly Mock<ILogger<PushNotificationStrategy>> _loggerMock;
    private readonly Mock<IOptions<PushConfig>> _optionsMock;
    private readonly PushNotificationStrategy _strategy;
    private readonly PushConfig _config;

    public PushNotificationStrategyTests()
    {
        _loggerMock = new Mock<ILogger<PushNotificationStrategy>>();
        _config = new PushConfig
        {
            FcmServerKey = "test-fcm-key",
            ApnsKeyId = "test-apns-key",
            ApnsTeamId = "test-team-id",
            ApnsAuthKey = "test-auth-key"
        };
        _optionsMock = new Mock<IOptions<PushConfig>>();
        _optionsMock.Setup(x => x.Value).Returns(_config);
        _strategy = new PushNotificationStrategy(_loggerMock.Object, _optionsMock.Object);
    }

    [Fact]
    public void Type_ReturnsPush()
    {
        // Act & Assert
        _strategy.Type.Should().Be(NotificationType.Push);
    }

    [Theory]
    [InlineData("fcm-token", "android")]
    [InlineData("apns-token", "ios")]
    public async Task SendAsync_WithValidMessage_ReturnsSuccessResult(string deviceToken, string platform)
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "deviceToken", deviceToken },
            { "platform", platform }
        };
        var message = TestDataBuilder.CreateValidNotificationMessage(metadata: metadata);

        // Act
        var result = await _strategy.SendAsync(message);

        // Assert
        result.Should().BeSuccessful();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Sending push notification to {platform}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithMissingDeviceToken_ReturnsFailureResult()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "platform", "android" }
        };
        var message = TestDataBuilder.CreateValidNotificationMessage(metadata: metadata);

        // Act
        var result = await _strategy.SendAsync(message);

        // Assert
        result.Should().BeFailure()
            .And.HaveError("Device token is required");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Device token is required")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithMissingPlatform_ReturnsFailureResult()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "deviceToken", "fcm-token" }
        };
        var message = TestDataBuilder.CreateValidNotificationMessage(metadata: metadata);

        // Act
        var result = await _strategy.SendAsync(message);

        // Assert
        result.Should().BeFailure()
            .And.HaveError("Platform is required");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Platform is required")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("web")]
    public async Task SendAsync_WithUnsupportedPlatform_ReturnsFailureResult(string platform)
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "deviceToken", "token" },
            { "platform", platform }
        };
        var message = TestDataBuilder.CreateValidNotificationMessage(metadata: metadata);

        // Act
        var result = await _strategy.SendAsync(message);

        // Assert
        result.Should().BeFailure()
            .And.HaveError($"Unsupported platform: {platform}");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Unsupported platform: {platform}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithMissingConfiguration_ReturnsFailureResult()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "deviceToken", "fcm-token" },
            { "platform", "android" }
        };
        var message = TestDataBuilder.CreateValidNotificationMessage(metadata: metadata);
        _optionsMock.Setup(x => x.Value).Returns(new PushConfig());

        // Act
        var result = await _strategy.SendAsync(message);

        // Assert
        result.Should().BeFailure()
            .And.HaveError("Push notification configuration is incomplete");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Push notification configuration is incomplete")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
