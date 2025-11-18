using Core.Models;
using FluentValidation;
using System.Text.RegularExpressions;

namespace NotificationService.Validators;

/// <summary>
/// Validator for SMS notifications.
/// </summary>
public class SmsMessageValidator : AbstractValidator<NotificationMessage>
{
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$",
        RegexOptions.Compiled);

    public SmsMessageValidator()
    {
        RuleFor(x => x.To)
            .NotEmpty().WithMessage("Phone number is required.")
            .Must(BeValidPhoneNumber).WithMessage("Invalid phone number format. Must be E.164 format.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Message content is required.")
            .MaximumLength(1600).WithMessage("Message cannot exceed 1600 characters (10 SMS segments).");

        // Subject validation removed - domain model ensures subject is provided by mapping
    }

    private static bool BeValidPhoneNumber(string phoneNumber)
        => !string.IsNullOrWhiteSpace(phoneNumber) && PhoneRegex.IsMatch(phoneNumber);
}
