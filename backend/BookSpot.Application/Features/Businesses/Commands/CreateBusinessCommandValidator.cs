using FluentValidation;

namespace BookSpot.Application.Features.Businesses.Commands;

public class CreateBusinessCommandValidator : AbstractValidator<CreateBusinessCommand>
{
    public CreateBusinessCommandValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty()
            .WithMessage("Business name is required.")
            .MaximumLength(100)
            .Must(value => value.All(character => !char.IsControl(character)))
            .WithMessage("Business name contains control characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Business description is required.")
            .MaximumLength(2000)
            .Must(value => value.All(character => character == '\n' || !char.IsControl(character)))
            .WithMessage("Business description contains invalid control characters.");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Business address is required.")
            .MaximumLength(250)
            .Must(value => value.All(character => !char.IsControl(character)))
            .WithMessage("Business address contains control characters.");

        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Business phone number is required.")
            .MaximumLength(32)
            .Matches(@"^\+?[0-9][0-9 ()-]{5,30}[0-9]$")
            .WithMessage("Phone number has an invalid format.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Business email is required.")
            .EmailAddress()
            .WithMessage("Business email must be a valid email address.")
            .MaximumLength(100)
            .WithMessage("Business email cannot exceed 100 characters.");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required.")
            .MaximumLength(100)
            .Must(value => value.All(character => !char.IsControl(character)))
            .WithMessage("City contains control characters.");

        RuleFor(x => x.Website)
            .Must(BeValidHttpsUrl)
            .When(x => !string.IsNullOrEmpty(x.Website))
            .WithMessage("Website must be an absolute HTTPS URL.");

        RuleFor(x => x.ImageUrl)
            .Must(BeValidHttpsUrl)
            .When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("Image URL must be an absolute HTTPS URL.");
    }

    private static bool BeValidHttpsUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)) return false;
        var allowedScheme = parsed.Scheme == Uri.UriSchemeHttps ||
            (parsed.Scheme == Uri.UriSchemeHttp && parsed.IsLoopback);
        return allowedScheme && string.IsNullOrEmpty(parsed.UserInfo) && string.IsNullOrEmpty(parsed.Fragment) &&
               parsed.IsDefaultPort && url!.Length <= 2048;
    }
}