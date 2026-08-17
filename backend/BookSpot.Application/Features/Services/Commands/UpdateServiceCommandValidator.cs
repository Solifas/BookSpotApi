using FluentValidation;

namespace BookSpot.Application.Features.Services.Commands;

public class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        // Only Id is required for updates
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Service ID is required.")
            .MaximumLength(128)
            .WithMessage("Service ID cannot exceed 128 characters.")
            .Must(id => id.All(character => !char.IsControl(character)))
            .WithMessage("Service ID contains invalid characters.");

        // All other fields are optional, but validate format when provided
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .Must(value => value!.All(character => !char.IsControl(character)))
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000)
            .Must(value => value!.All(character => character == '\n' || !char.IsControl(character)))
            .When(x => x.Description is not null);

        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(100)
            .Must(value => value!.All(character => !char.IsControl(character)))
            .When(x => x.Category is not null);

        RuleFor(x => x.Price)
            .InclusiveBetween(0m, 1_000_000m)
            .WithMessage("Service price must be between 0 and 1,000,000.")
            .PrecisionScale(9, 2, false)
            .WithMessage("Service price can have at most 2 decimal places.")
            .When(x => x.Price.HasValue);

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(15, 480)
            .WithMessage("Service duration must be between 15 and 480 minutes.")
            .Must(BeValidDuration)
            .WithMessage("Service duration should be in 15-minute increments.")
            .When(x => x.DurationMinutes.HasValue);

        RuleFor(x => x.ImageUrl)
            .Must(BeValidUrl)
            .WithMessage("Image URL must be a valid URL format.")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));

        RuleFor(x => x.Tags)
            .Must(HaveValidTags)
            .WithMessage("Tags must contain at most 20 unique values of 1-50 characters.")
            .When(x => x.Tags != null && x.Tags.Any());

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(100)
            .Must(value => value!.All(character => !char.IsControl(character)))
            .When(x => x.Location is not null);
    }


    private static bool BeValidDuration(int? durationMinutes)
    {
        return durationMinutes.HasValue && durationMinutes.Value % 15 == 0;
    }

    private static bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
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