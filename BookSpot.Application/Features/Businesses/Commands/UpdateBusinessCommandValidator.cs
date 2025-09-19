using FluentValidation;

namespace BookSpot.Application.Features.Businesses.Commands;

public class UpdateBusinessCommandValidator : AbstractValidator<UpdateBusinessCommand>
{
    public UpdateBusinessCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Business ID is required.")
            .Must(BeValidGuid)
            .WithMessage("Business ID must be a valid GUID format.");

        RuleFor(x => x.BusinessName)
            .NotEmpty()
            .WithMessage("Business name is required.")
            .Length(2, 100)
            .WithMessage("Business name must be between 2 and 100 characters.")
            .Matches(@"^[a-zA-Z0-9\s\-&'.,()]+$")
            .WithMessage("Business name contains invalid characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Business description is required.")
            .Length(10, 1000)
            .WithMessage("Business description must be between 10 and 1000 characters.");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Business address is required.")
            .Length(5, 200)
            .WithMessage("Business address must be between 5 and 200 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Business phone number is required.")
            .Length(10, 20)
            .WithMessage("Phone number must be between 10 and 20 characters.")
            .Matches(@"^[\+]?[0-9\s\-\(\)\.]+$")
            .WithMessage("Phone number contains invalid characters. Only numbers, spaces, hyphens, parentheses, periods, and plus sign are allowed.");

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
            .Length(2, 50)
            .WithMessage("City must be between 2 and 50 characters.")
            .Matches(@"^[a-zA-Z\s\-'.,()]+$")
            .WithMessage("City contains invalid characters.");

        RuleFor(x => x.Website)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrEmpty(x.Website))
            .WithMessage("Website must be a valid URL format.");

        RuleFor(x => x.ImageUrl)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("Image URL must be a valid URL format.");
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