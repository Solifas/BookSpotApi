using FluentValidation;

namespace BookSpot.Application.Features.Services.Commands;

public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        _ = RuleFor(x => x.BusinessId)
            .NotEmpty()
            .WithMessage("Business ID is required.")
            .Must(BeValidGuid)
            .WithMessage("Business ID must be a valid GUID format.");

        _ = RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Service name is required.")
            .Length(2, 100)
            .WithMessage("Service name must be between 2 and 100 characters.")
            .Matches(@"^[a-zA-Z0-9\s\-&'.,()]+$")
            .WithMessage("Service name contains invalid characters.");

        _ = RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Service price must be greater than 0.")
            .LessThanOrEqualTo(10000)
            .WithMessage("Service price cannot exceed $10,000.")
            .PrecisionScale(10, 2, false)
            .WithMessage("Service price can have at most 2 decimal places.");

        _ = RuleFor(x => x.DurationMinutes)
            .GreaterThan(0)
            .WithMessage("Service duration must be greater than 0 minutes.")
            .LessThanOrEqualTo(480)
            .WithMessage("Service duration cannot exceed 8 hours (480 minutes).")
            .Must(BeValidDuration)
            .WithMessage("Service duration should be in 15-minute increments.");
    }

    private static bool BeValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }

    private static bool BeValidDuration(int durationMinutes)
    {
        // Allow durations in 15-minute increments
        return durationMinutes % 15 == 0;
    }
}