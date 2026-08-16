using FluentValidation;

namespace BookSpot.Application.Features.Bookings.Commands;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty()
            .WithMessage("Service ID is required.")
            .MaximumLength(128)
            .WithMessage("Service ID cannot exceed 128 characters.");

        RuleFor(x => x.StartTime)
            .NotEmpty()
            .WithMessage("Start time is required.")
            .Must(BeFutureDate)
            .WithMessage("Start time must be in the future.")
            .Must(BeReasonableFutureDate)
            .WithMessage("Start time cannot be more than 1 year in the future.")
            .Must(BeOnValidTime)
            .WithMessage("Start time should be on 15-minute intervals (e.g., 09:00, 09:15, 09:30, 09:45).");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MinimumLength(16)
            .MaximumLength(128);
    }

    private static bool BeFutureDate(DateTimeOffset startTime)
    {
        return startTime > DateTime.UtcNow.AddMinutes(30); // Allow at least 30 minutes advance booking
    }

    private static bool BeReasonableFutureDate(DateTimeOffset startTime)
    {
        return startTime <= DateTime.UtcNow.AddYears(1);
    }

    private static bool BeOnValidTime(DateTimeOffset startTime)
    {
        // Check if the time is on 15-minute intervals
        return startTime.Minute % 15 == 0 && startTime.Second == 0 && startTime.Millisecond == 0;
    }

}