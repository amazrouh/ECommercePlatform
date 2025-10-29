using Core.Models;
using FluentValidation;
using System.Text.Json;

namespace NotificationService.Validators;

/// <summary>
/// Validator for push notifications.
/// </summary>
public class PushMessageValidator : AbstractValidator<NotificationMessage>
{
    public PushMessageValidator()
    {
        RuleFor(x => x.To)
            .NotEmpty().WithMessage("Device token is required.");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Notification title is required.")
            .MaximumLength(150).WithMessage("Title cannot exceed 150 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Notification body is required.")
            .MaximumLength(2000).WithMessage("Body cannot exceed 2000 characters.");

        RuleFor(x => x.Metadata)
            .Must(HaveValidPayload)
            .When(x => x.Metadata.Any())
            .WithMessage("Push notification payload is invalid or too large.");
    }

    private static bool HaveValidPayload(IReadOnlyDictionary<string, object> metadata)
    {
        try
        {
            // Serialize metadata to check payload size
            var json = JsonSerializer.Serialize(metadata);
            return json.Length <= 4096; // FCM payload limit
        }
        catch
        {
            return false;
        }
    }
}
