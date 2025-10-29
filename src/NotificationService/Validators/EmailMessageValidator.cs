using Core.Models;
using FluentValidation;
using System.Text.RegularExpressions;

namespace NotificationService.Validators;

/// <summary>
/// Validator for email notifications.
/// </summary>
public class EmailMessageValidator : AbstractValidator<NotificationMessage>
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public EmailMessageValidator()
    {
        RuleFor(x => x.To)
            .NotEmpty().WithMessage("Email address is required.")
            .Must(BeValidEmail).WithMessage("Invalid email address format.");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required.")
            .MaximumLength(200).WithMessage("Subject cannot exceed 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .MaximumLength(100000).WithMessage("Body cannot exceed 100,000 characters.");
    }

    private static bool BeValidEmail(string email)
        => !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);
}
