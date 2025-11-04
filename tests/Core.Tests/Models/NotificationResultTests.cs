using Core.Models;
using FluentAssertions;

namespace Core.Tests.Models;

public class NotificationResultTests
{
    [Fact]
    public void Succeeded_CreatesSuccessfulResult()
    {
        // Arrange
        var messageId = "test-message-id";
        var sentAt = DateTimeOffset.UtcNow;

        // Act
        var result = NotificationResult.Succeeded(messageId, sentAt);

        // Assert
        result.Success.Should().BeTrue();
        result.MessageId.Should().Be(messageId);
        result.SentAt.Should().Be(sentAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Succeeded_WithInvalidMessageId_ThrowsArgumentException(string messageId)
    {
        // Act
        var act = () => NotificationResult.Succeeded(messageId, DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Message ID cannot be empty*");
    }

    [Fact]
    public void Failed_CreatesFailureResult()
    {
        // Arrange
        var error = "Test error message";

        // Act
        var result = NotificationResult.Failed(error);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Failed_WithInvalidError_ThrowsArgumentException(string error)
    {
        // Act
        var act = () => NotificationResult.Failed(error);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Error message cannot be empty*");
    }

    [Fact]
    public void SuccessfulResults_WithSameData_AreEqual()
    {
        // Arrange
        var messageId = "test-message-id";
        var sentAt = DateTimeOffset.UtcNow;

        // Act
        var result1 = NotificationResult.Succeeded(messageId, sentAt);
        var result2 = NotificationResult.Succeeded(messageId, sentAt);

        // Assert
        result1.Should().BeEquivalentTo(result2);
        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }

    [Fact]
    public void FailedResults_WithSameData_AreEqual()
    {
        // Arrange
        var error = "Test error message";

        // Act
        var result1 = NotificationResult.Failed(error);
        var result2 = NotificationResult.Failed(error);

        // Assert
        result1.Should().BeEquivalentTo(result2);
        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }
}
