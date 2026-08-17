using FluentValidation;

namespace BookSpot.Application.Features.Businesses.Commands;

public class UpdateBusinessCommandValidator : AbstractValidator<UpdateBusinessCommand>
{
    public UpdateBusinessCommandValidator()
    {
        // Only Id is required for updates
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Business ID is required.")
            .MaximumLength(128)
            .WithMessage("Business ID cannot exceed 128 characters.")
            .Must(id => id.All(character => !char.IsControl(character)))
            .WithMessage("Business ID contains invalid characters.");

        // All other fields are optional, but validate format when provided
        RuleFor(x => x.BusinessName)
            .NotEmpty()
            .MaximumLength(100)
            .Must(value => value!.All(character => !char.IsControl(character)))
            .When(x => x.BusinessName is not null);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000)
            .Must(value => value!.All(character => character == '\n' || !char.IsControl(character)))
            .When(x => x.Description is not null);

        RuleFor(x => x.Address)
            .NotEmpty()
            .MaximumLength(250)
            .Must(value => value!.All(character => !char.IsControl(character)))
            .When(x => x.Address is not null);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(32)
            .Matches(@"^\+?[0-9][0-9 ()-]{5,30}[0-9]$")
            .WithMessage("Phone number has an invalid format.")
            .When(x => x.Phone is not null);

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Business email must be a valid email address.")
            .MaximumLength(100)
            .WithMessage("Business email cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100)
            .Must(value => value!.All(character => !char.IsControl(character)))
            .When(x => x.City is not null);

        RuleFor(x => x.Website)
            .Must(BeValidHttpsUrl)
            .WithMessage("Website must be an absolute HTTPS URL.")
            .When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.ImageUrl)
            .Must(BeValidHttpsUrl)
            .WithMessage("Image URL must be an absolute HTTPS URL.")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));
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
