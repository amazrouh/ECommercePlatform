using Core.Enums;
using Core.Interfaces;
using Core.Models;
using NotificationService.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Decorators;

namespace NotificationService.UnitTests.Decorators;

public class RetryNotificationDecoratorTests
{
    private readonly Mock<INotificationService> _innerMock;
    private readonly Mock<ILogger<RetryNotificationDecorator>> _loggerMock;
    private readonly RetryNotificationDecorator _decorator;

    public RetryNotificationDecoratorTests()
    {
        _innerMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<RetryNotificationDecorator>>();
        _decorator = new RetryNotificationDecorator(_innerMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SendAsync_SucceedsOnFirstTry_NoRetry()
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
        _innerMock.Verify(x => x.SendAsync(type, message, default), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry attempt")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_SucceedsAfterRetries()
    {
        // Arrange
        var type = NotificationType.Email;
        var message = TestDataBuilder.CreateValidNotificationMessage();
        var result = TestDataBuilder.CreateSuccessResult();
        var exception = new TimeoutException("Test timeout");

        var callCount = 0;
        _innerMock.Setup(x => x.SendAsync(type, message, default))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw exception;
                }
                return result;
            });

        // Act
        var actualResult = await _decorator.SendAsync(type, message);

        // Assert
        actualResult.Should().Be(result);
        _innerMock.Verify(x => x.SendAsync(type, message, default), Times.Exactly(3));
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry attempt")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task SendAsync_FailsAfterAllRetries()
    {
        // Arrange
        var type = NotificationType.Email;
        var message = TestDataBuilder.CreateValidNotificationMessage();
        var exception = new TimeoutException("Test timeout");

        _innerMock.Setup(x => x.SendAsync(type, message, default))
            .ThrowsAsync(exception);

        // Act
        var actualResult = await _decorator.SendAsync(type, message);

        // Assert
        actualResult.Success.Should().BeFalse();
        actualResult.Error.Should().Be(exception.Message);
        _innerMock.Verify(x => x.SendAsync(type, message, default), Times.Exactly(4)); // Initial + 3 retries
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry attempt")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task SendBatchAsync_SucceedsAfterRetries()
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

        var exception = new TimeoutException("Test timeout");

        var callCount = 0;
        _innerMock.Setup(x => x.SendBatchAsync(notifications, default))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    throw exception;
                }
                return results;
            });

        // Act
        var actualResults = await _decorator.SendBatchAsync(notifications);

        // Assert
        actualResults.Should().BeEquivalentTo(results);
        _innerMock.Verify(x => x.SendBatchAsync(notifications, default), Times.Exactly(2));
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Batch retry attempt")),
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
