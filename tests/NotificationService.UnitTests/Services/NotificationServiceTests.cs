using Core.Enums;
using Core.Interfaces;
using Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Services;
using NotificationService.UnitTests.TestHelpers;

namespace NotificationService.UnitTests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationStrategyFactory> _factoryMock;
    private readonly Mock<ILogger<NotificationService.Services.NotificationService>> _loggerMock;
    private readonly Mock<INotificationStrategy> _strategyMock;
    private readonly Mock<IAuditLogger> _auditLoggerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly NotificationService.Services.NotificationService _service;

    public NotificationServiceTests()
    {
        _factoryMock = new Mock<INotificationStrategyFactory>();
        _loggerMock = new Mock<ILogger<NotificationService.Services.NotificationService>>();
        _strategyMock = new Mock<INotificationStrategy>();
        _auditLoggerMock = new Mock<IAuditLogger>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _service = new NotificationService.Services.NotificationService(
            _factoryMock.Object,
            _loggerMock.Object,
            _auditLoggerMock.Object,
            _httpContextAccessorMock.Object);
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
        result.Success.Should().BeTrue();
        result.Should().Be(expectedResult);
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
        result.Success.Should().BeFalse();
        result.Error.Should().Be(exception.Message);
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
        results[NotificationType.Email].Success.Should().BeTrue();
        results[NotificationType.Email].Should().Be(emailResult);
        results[NotificationType.Sms].Success.Should().BeTrue();
        results[NotificationType.Sms].Should().Be(smsResult);
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
        results[NotificationType.Email].Success.Should().BeTrue();
        results[NotificationType.Email].Should().Be(emailResult);
        results[NotificationType.Sms].Success.Should().BeFalse();
        results[NotificationType.Sms].Error.Should().Be(exception.Message);
    }

    [Fact]
    public async Task GetSupportedTypes_ReturnsSupportedTypes()
    {
        // Arrange
        var expectedTypes = new[] { NotificationType.Email, NotificationType.Sms };

        // Act
        var types = await _service.GetSupportedTypes();

        // Assert
        types.Should().BeEquivalentTo(expectedTypes);
    }
}
