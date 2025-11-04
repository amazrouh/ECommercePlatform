using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Decorators;
using Polly.CircuitBreaker;

namespace NotificationService.UnitTests.Decorators;

public class CircuitBreakerNotificationDecoratorTests
{
    private readonly Mock<INotificationService> _innerMock;
    private readonly Mock<ILogger<CircuitBreakerNotificationDecorator>> _loggerMock;
    private readonly CircuitBreakerNotificationDecorator _decorator;

    public CircuitBreakerNotificationDecoratorTests()
    {
        _innerMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<CircuitBreakerNotificationDecorator>>();
        _decorator = new CircuitBreakerNotificationDecorator(_innerMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SendAsync_WhenCircuitClosed_CallsInner()
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
    }

    [Fact]
    public async Task SendAsync_WhenCircuitOpens_ReturnsFailure()
    {
        // Arrange
        var type = NotificationType.Email;
        var message = TestDataBuilder.CreateValidNotificationMessage();
        var exception = new Exception("Test error");

        // Trigger circuit breaker by failing multiple times
        _innerMock.Setup(x => x.SendAsync(type, message, default))
            .ThrowsAsync(exception);

        // Act & Assert
        for (var i = 0; i < 6; i++) // 5 failures needed to open circuit
        {
            var result = await _decorator.SendAsync(type, message);
            result.Success.Should().BeFalse();
        }

        _innerMock.Verify(x => x.SendAsync(type, message, default), Times.Exactly(5));
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Circuit breaker tripped")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendBatchAsync_WhenCircuitClosed_CallsInner()
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
        _innerMock.Verify(x => x.SendBatchAsync(notifications, default), Times.Once);
    }

    [Fact]
    public async Task SendBatchAsync_WhenCircuitOpens_ReturnsFailure()
    {
        // Arrange
        var notifications = new Dictionary<NotificationType, NotificationMessage>
        {
            [NotificationType.Email] = TestDataBuilder.CreateValidNotificationMessage(),
            [NotificationType.Sms] = TestDataBuilder.CreateValidNotificationMessage()
        };

        var exception = new Exception("Test error");

        // Trigger circuit breaker by failing multiple times
        _innerMock.Setup(x => x.SendBatchAsync(notifications, default))
            .ThrowsAsync(exception);

        // Act & Assert
        for (var i = 0; i < 6; i++) // 5 failures needed to open circuit
        {
            var results = await _decorator.SendBatchAsync(notifications);
            results.Should().HaveCount(2);
            results.Values.Should().AllSatisfy(r => r.Success.Should().BeFalse());
        }

        _innerMock.Verify(x => x.SendBatchAsync(notifications, default), Times.Exactly(5));
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Batch circuit breaker tripped")),
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
