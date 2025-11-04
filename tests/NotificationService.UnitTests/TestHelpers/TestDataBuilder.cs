using Core.Enums;
using Core.Models;

namespace NotificationService.UnitTests.TestHelpers;

/// <summary>
/// Builder pattern for creating test data.
/// </summary>
public static class TestDataBuilder
{
    public static NotificationMessage CreateValidNotificationMessage(
        string? to = null,
        string? subject = null,
        string? body = null,
        IDictionary<string, object>? metadata = null)
    {
        return new NotificationMessage(
            to ?? "test@example.com",
            subject ?? "Test Subject",
            body ?? "Test Body",
            metadata);
    }

    public static NotificationResult CreateSuccessResult(
        string messageId = "test-message-id",
        DateTimeOffset? sentAt = null)
    {
        return NotificationResult.Succeeded(messageId, sentAt ?? DateTimeOffset.UtcNow);
    }

    public static NotificationResult CreateFailureResult(
        string error = "Test error message")
    {
        return NotificationResult.Failed(error);
    }
}
