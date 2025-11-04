using Core.Enums;
using Core.Interfaces;
using Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Decorators;
using NotificationService.UnitTests.TestHelpers;

namespace NotificationService.UnitTests.Decorators;

public class LoggingNotificationDecoratorTests
{
    private readonly Mock<INotificationService> _innerMock;
    private readonly Mock<ILogger<LoggingNotificationDecorator>> _loggerMock;
    private readonly LoggingNotificationDecorator _decorator;

    public LoggingNotificationDecoratorTests()
    {
        _innerMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<LoggingNotificationDecorator>>();
        _decorator = new LoggingNotificationDecorator(_innerMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SendAsync_LogsBeforeAndAfterSuccess()
    {
        // Arrange
        var type = NotificationType.Email;
        var message = TestDataBuilder.CreateValidNotificationMessage();
        var result = TestDataBuilder.CreateSuccessResult();

        _innerMock.Setup(x => x.SendAsync(type, message, default))
            .ReturnsAsync(result);

        // Act
        var actualResult = await _decorator.SendAsync(type, message);

        // Assert
        actualResult.Should().Be(result);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending notification")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Notification sent successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_LogsBeforeAndAfterFailure()
    {
        // Arrange
        var type = NotificationType.Email;
        var message = TestDataBuilder.CreateValidNotificationMessage();
        var result = TestDataBuilder.CreateFailureResult("Test error");

        _innerMock.Setup(x => x.SendAsync(type, message, default))
            .ReturnsAsync(result);

        // Act
        var actualResult = await _decorator.SendAsync(type, message);

        // Assert
        actualResult.Should().Be(result);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending notification")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Notification failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendBatchAsync_LogsBeforeAndAfterSuccess()
    {
        // Arrange
        var notifications = new Dictionary<NotificationType, NotificationMessage>
        {
            [NotificationType.Email] = TestDataBuilder.CreateValidNotificationMessage(),
            [NotificationType.Sms] = TestDataBuilder.CreateValidNotificationMessage()
        };

        var results = new Dictionary<NotificationType, NotificationResult>
        {
            [NotificationType.Email] = TestDataBuilder.CreateSuccessResult("email-id"),
            [NotificationType.Sms] = TestDataBuilder.CreateSuccessResult("sms-id")
        };

        _innerMock.Setup(x => x.SendBatchAsync(notifications, default))
            .ReturnsAsync(results);

        // Act
        var actualResults = await _decorator.SendBatchAsync(notifications);

        // Assert
        actualResults.Should().BeEquivalentTo(results);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending batch of 2 notifications")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Batch notification completed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSupportedTypes_DelegatesCall()
    {
        // Arrange
        var expectedTypes = new[] { NotificationType.Email, NotificationType.Sms };
        _innerMock.Setup(x => x.GetSupportedTypes())
            .Returns(Task.FromResult<IEnumerable<NotificationType>>(expectedTypes));

        // Act
        var types = await _decorator.GetSupportedTypes();

        // Assert
        types.Should().BeEquivalentTo(expectedTypes);
        _innerMock.Verify(x => x.GetSupportedTypes(), Times.Once);
    }
}
