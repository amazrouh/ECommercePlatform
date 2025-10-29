using Core.Enums;
using Core.Interfaces;
using Core.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Services;

namespace NotificationService.UnitTests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationStrategyFactory> _factoryMock;
    private readonly Mock<ILogger<NotificationService.Services.NotificationService>> _loggerMock;
    private readonly Mock<INotificationStrategy> _strategyMock;
    private readonly NotificationService.Services.NotificationService _service;

    public NotificationServiceTests()
    {
        _factoryMock = new Mock<INotificationStrategyFactory>();
        _loggerMock = new Mock<ILogger<NotificationService.Services.NotificationService>>();
        _strategyMock = new Mock<INotificationStrategy>();
        _service = new NotificationService.Services.NotificationService(_factoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SendAsync_WithValidMessage_ReturnsSuccessResult()
    {
        // Arrange
        var type = NotificationType.Email;
        var message = TestDataBuilder.CreateValidNotificationMessage();
        var expectedResult = TestDataBuilder.CreateSuccessResult();

        _strategyMock.Setup(x => x.SendAsync(message, default))
            .ReturnsAsync(expectedResult);
        _factoryMock.Setup(x => x.GetStrategy(type))
            .Returns(_strategyMock.Object);

        // Act
        var result = await _service.SendAsync(type, message);

        // Assert
        result.Should().BeSuccessful()
            .And.Be(expectedResult);
        _factoryMock.Verify(x => x.GetStrategy(type), Times.Once);
        _strategyMock.Verify(x => x.SendAsync(message, default), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WhenStrategyThrowsException_ReturnsFailureResult()
    {
        // Arrange
        var type = NotificationType.Email;
        var message = TestDataBuilder.CreateValidNotificationMessage();
        var exception = new Exception("Test error");

        _strategyMock.Setup(x => x.SendAsync(message, default))
            .ThrowsAsync(exception);
        _factoryMock.Setup(x => x.GetStrategy(type))
            .Returns(_strategyMock.Object);

        // Act
        var result = await _service.SendAsync(type, message);

        // Assert
        result.Should().BeFailure()
            .And.HaveError(exception.Message);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(exception.Message)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendBatchAsync_WithValidMessages_ReturnsResults()
    {
        // Arrange
        var notifications = new Dictionary<NotificationType, NotificationMessage>
        {
            [NotificationType.Email] = TestDataBuilder.CreateValidNotificationMessage(),
            [NotificationType.Sms] = TestDataBuilder.CreateValidNotificationMessage()
        };

        var emailResult = TestDataBuilder.CreateSuccessResult("email-id");
        var smsResult = TestDataBuilder.CreateSuccessResult("sms-id");

        var emailStrategyMock = new Mock<INotificationStrategy>();
        emailStrategyMock.Setup(x => x.SendAsync(notifications[NotificationType.Email], default))
            .ReturnsAsync(emailResult);

        var smsStrategyMock = new Mock<INotificationStrategy>();
        smsStrategyMock.Setup(x => x.SendAsync(notifications[NotificationType.Sms], default))
            .ReturnsAsync(smsResult);

        _factoryMock.Setup(x => x.GetStrategy(NotificationType.Email))
            .Returns(emailStrategyMock.Object);
        _factoryMock.Setup(x => x.GetStrategy(NotificationType.Sms))
            .Returns(smsStrategyMock.Object);

        // Act
        var results = await _service.SendBatchAsync(notifications);

        // Assert
        results.Should().HaveCount(2);
        results[NotificationType.Email].Should().BeSuccessful()
            .And.Be(emailResult);
        results[NotificationType.Sms].Should().BeSuccessful()
            .And.Be(smsResult);
    }

    [Fact]
    public async Task SendBatchAsync_WhenSomeStrategiesFail_ReturnsMixedResults()
    {
        // Arrange
        var notifications = new Dictionary<NotificationType, NotificationMessage>
        {
            [NotificationType.Email] = TestDataBuilder.CreateValidNotificationMessage(),
            [NotificationType.Sms] = TestDataBuilder.CreateValidNotificationMessage()
        };

        var emailResult = TestDataBuilder.CreateSuccessResult("email-id");
        var exception = new Exception("SMS error");

        var emailStrategyMock = new Mock<INotificationStrategy>();
        emailStrategyMock.Setup(x => x.SendAsync(notifications[NotificationType.Email], default))
            .ReturnsAsync(emailResult);

        var smsStrategyMock = new Mock<INotificationStrategy>();
        smsStrategyMock.Setup(x => x.SendAsync(notifications[NotificationType.Sms], default))
            .ThrowsAsync(exception);

        _factoryMock.Setup(x => x.GetStrategy(NotificationType.Email))
            .Returns(emailStrategyMock.Object);
        _factoryMock.Setup(x => x.GetStrategy(NotificationType.Sms))
            .Returns(smsStrategyMock.Object);

        // Act
        var results = await _service.SendBatchAsync(notifications);

        // Assert
        results.Should().HaveCount(2);
        results[NotificationType.Email].Should().BeSuccessful()
            .And.Be(emailResult);
        results[NotificationType.Sms].Should().BeFailure()
            .And.HaveError(exception.Message);
    }

    [Fact]
    public void GetSupportedTypes_ReturnsSupportedTypes()
    {
        // Arrange
        var expectedTypes = new[] { NotificationType.Email, NotificationType.Sms };
        _factoryMock.Setup(x => x.GetSupportedTypes())
            .Returns(expectedTypes);

        // Act
        var types = _service.GetSupportedTypes();

        // Assert
        types.Should().BeEquivalentTo(expectedTypes);
        _factoryMock.Verify(x => x.GetSupportedTypes(), Times.Once);
    }
}
