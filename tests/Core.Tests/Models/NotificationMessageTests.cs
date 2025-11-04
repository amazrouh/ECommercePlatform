using Core.Models;
using Core.TestHelpers;
using FluentAssertions;

namespace Core.Tests.Models;

public class NotificationMessageTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesInstance()
    {
        // Arrange
        var to = "test@example.com";
        var subject = "Test Subject";
        var body = "Test Body";
        var metadata = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var message = new NotificationMessage(to, subject, body, metadata);

        // Assert
        message.To.Should().Be(to);
        message.Subject.Should().Be(subject);
        message.Body.Should().Be(body);
        message.Metadata.Should().ContainKey("key").And.ContainValue("value");
    }

    [Theory]
    [InlineData(null, "Subject", "Body", "Recipient cannot be empty")]
    [InlineData("", "Subject", "Body", "Recipient cannot be empty")]
    [InlineData("  ", "Subject", "Body", "Recipient cannot be empty")]
    [InlineData("test@example.com", null, "Body", "Subject cannot be empty")]
    [InlineData("test@example.com", "", "Body", "Subject cannot be empty")]
    [InlineData("test@example.com", "  ", "Body", "Subject cannot be empty")]
    [InlineData("test@example.com", "Subject", null, "Body cannot be empty")]
    [InlineData("test@example.com", "Subject", "", "Body cannot be empty")]
    [InlineData("test@example.com", "Subject", "  ", "Body cannot be empty")]
    public void Constructor_WithInvalidData_ThrowsArgumentException(string to, string subject, string body, string expectedError)
    {
        // Act
        var act = () => new NotificationMessage(to, subject, body);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"*{expectedError}*");
    }

    [Fact]
    public void WithMetadata_ReturnsNewInstanceWithUpdatedMetadata()
    {
        // Arrange
        var message = TestDataBuilder.CreateValidNotificationMessage();
        var newMetadata = new Dictionary<string, object> { { "newKey", "newValue" } };

        // Act
        var updatedMessage = message.WithMetadata(newMetadata);

        // Assert
        updatedMessage.Should().NotBeSameAs(message);
        updatedMessage.To.Should().Be(message.To);
        updatedMessage.Subject.Should().Be(message.Subject);
        updatedMessage.Body.Should().Be(message.Body);
        updatedMessage.Metadata.Should().ContainKey("newKey").And.ContainValue("newValue");
    }

    [Fact]
    public void Metadata_IsImmutable()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { { "key", "value" } };
        var message = TestDataBuilder.CreateValidNotificationMessage(metadata: metadata);

        // Act
        metadata["key"] = "changed";
        metadata["newKey"] = "newValue";

        // Assert
        message.Metadata.Should().HaveCount(1);
        message.Metadata["key"].Should().Be("value");
        message.Metadata.Should().NotContainKey("newKey");
    }
}
