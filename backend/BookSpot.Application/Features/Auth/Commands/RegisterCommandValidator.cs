using FluentValidation;

namespace BookSpot.Application.Features.Auth.Commands;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.")
            .MaximumLength(100)
            .WithMessage("Email cannot exceed 100 characters.");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required.")
            .Length(2, 100)
            .WithMessage("Full name must be between 2 and 100 characters.")
            .Matches(@"^[a-zA-Z\s\-'.,()]+$")
            .WithMessage("Full name contains invalid characters. Only letters, spaces, hyphens, apostrophes, periods, commas, and parentheses are allowed.");

        RuleFor(x => x.ContactNumber)
            .Length(10, 20)
            .When(x => !string.IsNullOrEmpty(x.ContactNumber))
            .WithMessage("Contact number must be between 10 and 20 characters when provided.")
            .Matches(@"^[\+]?[0-9\s\-\(\)\.]+$")
            .When(x => !string.IsNullOrEmpty(x.ContactNumber))
            .WithMessage("Contact number contains invalid characters. Only numbers, spaces, hyphens, parentheses, periods, and plus sign are allowed.");

        RuleFor(x => x.Password)
            .Must(AuthRules.IsPasswordAllowed)
            .WithMessage("Password does not satisfy the password policy.");

        RuleFor(x => x.UserType)
            .NotEmpty()
            .WithMessage("User type is required.")
            .Must(BeValidUserType)
            .WithMessage("User type must be either 'client' or 'provider'.");
    }

    private static bool BeValidUserType(string userType)
    {
        return userType == "client" || userType == "provider";
    }
}