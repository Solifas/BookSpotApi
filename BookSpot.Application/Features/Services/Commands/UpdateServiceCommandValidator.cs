using FluentValidation;

namespace BookSpot.Application.Features.Services.Commands;

public class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Service ID is required.")
            .Must(BeValidGuid)
            .WithMessage("Service ID must be a valid GUID format.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Service name is required.")
            .Length(2, 100)
            .WithMessage("Service name must be between 2 and 100 characters.")
            .Matches(@"^[a-zA-Z0-9\s\-&'.,()]+$")
            .WithMessage("Service name contains invalid characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Service description is required.")
            .Length(10, 500)
            .WithMessage("Service description must be between 10 and 500 characters.");

        // RuleFor(x => x.Category)
        //     .NotEmpty()
        //     .WithMessage("Service category is required.")
        //     .Length(2, 50)
        //     .WithMessage("Service category must be between 2 and 50 characters.")
        //     .Matches(@"^[a-zA-Z\s\-&'.,()]+$")
        //     .WithMessage("Service category contains invalid characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Service price must be greater than 0.")
            .LessThanOrEqualTo(10000)
            .WithMessage("Service price cannot exceed $10,000.")
            .PrecisionScale(10, 2, false)
            .WithMessage("Service price can have at most 2 decimal places.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0)
            .WithMessage("Service duration must be greater than 0 minutes.")
            .LessThanOrEqualTo(480)
            .WithMessage("Service duration cannot exceed 8 hours (480 minutes).")
            .Must(BeValidDuration)
            .WithMessage("Service duration should be in 15-minute increments.");

        RuleFor(x => x.ImageUrl)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("Image URL must be a valid URL format.");

        RuleFor(x => x.Tags)
            .Must(HaveValidTags)
            .When(x => x.Tags != null && x.Tags.Any())
            .WithMessage("Tags must be between 2 and 30 characters each and contain only letters, numbers, spaces, and hyphens.");
    }

    private static bool BeValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }

    private static bool BeValidDuration(int durationMinutes)
    {
        return durationMinutes % 15 == 0;
    }

    private static bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    private static bool HaveValidTags(List<string>? tags)
    {
        if (tags == null) return true;

        return tags.All(tag =>
            !string.IsNullOrWhiteSpace(tag) &&
            tag.Length >= 2 &&
            tag.Length <= 30 &&
            System.Text.RegularExpressions.Regex.IsMatch(tag, @"^[a-zA-Z0-9\s\-]+$"));
    }
}