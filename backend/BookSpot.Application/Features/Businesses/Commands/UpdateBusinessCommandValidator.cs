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
            .Must(BeValidGuid)
            .WithMessage("Business ID must be a valid GUID format.");

        // All other fields are optional, but validate format when provided
        RuleFor(x => x.BusinessName)
            .Length(2, 100)
            .WithMessage("Business name must be between 2 and 100 characters.")
            .Matches(@"^[a-zA-Z0-9\s\-&'.,()]+$")
            .WithMessage("Business name contains invalid characters.")
            .When(x => !string.IsNullOrEmpty(x.BusinessName));

        RuleFor(x => x.Description)
            .Length(10, 1000)
            .WithMessage("Business description must be between 10 and 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Address)
            .Length(5, 200)
            .WithMessage("Business address must be between 5 and 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.Phone)
            .Length(10, 20)
            .WithMessage("Phone number must be between 10 and 20 characters.")
            .Matches(@"^[\+]?[0-9\s\-\(\)\.]+$")
            .WithMessage("Phone number contains invalid characters. Only numbers, spaces, hyphens, parentheses, periods, and plus sign are allowed.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Business email must be a valid email address.")
            .MaximumLength(100)
            .WithMessage("Business email cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.City)
            .Length(2, 50)
            .WithMessage("City must be between 2 and 50 characters.")
            .Matches(@"^[a-zA-Z\s\-'.,()]+$")
            .WithMessage("City contains invalid characters.")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.Website)
            .Must(BeValidUrl)
            .WithMessage("Website must be a valid URL format.")
            .When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.ImageUrl)
            .Must(BeValidUrl)
            .WithMessage("Image URL must be a valid URL format.")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));
    }

    private static bool BeValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }

    private static bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
