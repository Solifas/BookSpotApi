using FluentValidation;

namespace BookSpot.Application.Features.Services.Commands;

public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        _ = RuleFor(x => x.BusinessId)
            .NotEmpty()
            .WithMessage("Business ID is required.")
            .Must(BeValidOpaqueId)
            .WithMessage("Business ID must be 1-128 UTF-8 bytes without control characters.");

        _ = RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Service name is required.")
            .MaximumLength(100)
            .WithMessage("Service name cannot exceed 100 characters.")
            .Must(value => value.All(character => !char.IsControl(character)))
            .WithMessage("Service name contains control characters.");

        _ = RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Service description is required.")
            .MaximumLength(2000)
            .WithMessage("Service description cannot exceed 2000 characters.")
            .Must(HaveValidDescriptionCharacters)
            .WithMessage("Service description contains invalid control characters.");

        _ = RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(100)
            .Must(value => value!.All(character => !char.IsControl(character)))
            .When(x => x.Category is not null);

        _ = RuleFor(x => x.Price)
            .InclusiveBetween(0m, 1_000_000m)
            .WithMessage("Service price must be between 0 and 1,000,000.")
            .PrecisionScale(9, 2, false)
            .WithMessage("Service price can have at most 2 decimal places.");

        _ = RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(15, 480)
            .WithMessage("Service duration must be between 15 and 480 minutes.")
            .Must(BeValidDuration)
            .WithMessage("Service duration must be in 15-minute increments.");

        _ = RuleFor(x => x.ImageUrl)
            .Must(BeValidHttpsUrl)
            .When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("Image URL must be an absolute HTTPS URL.");

        _ = RuleFor(x => x.Tags)
            .Must(HaveValidTags)
            .When(x => x.Tags != null && x.Tags.Any())
            .WithMessage("Tags must contain at most 20 unique values of 1-50 characters.");

        _ = RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(100)
            .Must(value => value!.All(character => !char.IsControl(character)))
            .When(x => x.Location is not null);
    }

    private static bool BeValidOpaqueId(string id) =>
        System.Text.Encoding.UTF8.GetByteCount(id) <= 128 && id.All(character => !char.IsControl(character));

    private static bool HaveValidDescriptionCharacters(string value) =>
        value.All(character => character == '\n' || !char.IsControl(character));

    private static bool BeValidDuration(int durationMinutes)
    {
        // Allow durations in 15-minute increments
        return durationMinutes % 15 == 0;
    }

    private static bool BeValidHttpsUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)) return false;
        var allowedScheme = parsed.Scheme == Uri.UriSchemeHttps ||
            (parsed.Scheme == Uri.UriSchemeHttp && parsed.IsLoopback);
        return allowedScheme && string.IsNullOrEmpty(parsed.UserInfo) && string.IsNullOrEmpty(parsed.Fragment) &&
               parsed.IsDefaultPort && url!.Length <= 2048;
    }

    private static bool HaveValidTags(List<string>? tags)
    {
        if (tags == null) return true;

        return tags.Count <= 20 &&
               tags.All(tag => !string.IsNullOrWhiteSpace(tag) && tag.Length <= 50 &&
                               tag.All(character => !char.IsControl(character))) &&
               tags.Distinct(StringComparer.OrdinalIgnoreCase).Count() == tags.Count;
    }
}