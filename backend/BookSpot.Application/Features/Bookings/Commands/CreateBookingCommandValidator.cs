using FluentValidation;

namespace BookSpot.Application.Features.Bookings.Commands;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty()
            .WithMessage("Service ID is required.")
            .Must(BeValidGuid)
            .WithMessage("Service ID must be a valid GUID format.");

        RuleFor(x => x.StartTime)
            .NotEmpty()
            .WithMessage("Start time is required.")
            .Must(BeFutureDate)
            .WithMessage("Start time must be in the future.")
            .Must(BeReasonableFutureDate)
            .WithMessage("Start time cannot be more than 1 year in the future.")
            .Must(BeOnValidTime)
            .WithMessage("Start time should be on 15-minute intervals (e.g., 09:00, 09:15, 09:30, 09:45).");

        RuleFor(x => x.EndTime)
            .Must((command, endTime) => BeValidEndTime(command, endTime))
            .When(x => x.EndTime != default)
            .WithMessage("End time must be after start time and within reasonable duration.");

        RuleFor(x => x.StartTime)
            .Must(BeWithinBusinessHours)
            .WithMessage("Booking must be scheduled during reasonable business hours (6 AM - 11 PM).");
    }

    private static bool BeValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }

    private static bool BeFutureDate(DateTime startTime)
    {
        return startTime > DateTime.UtcNow.AddMinutes(30); // Allow at least 30 minutes advance booking
    }

    private static bool BeReasonableFutureDate(DateTime startTime)
    {
        return startTime <= DateTime.UtcNow.AddYears(1);
    }

    private static bool BeOnValidTime(DateTime startTime)
    {
        // Check if the time is on 15-minute intervals
        return startTime.Minute % 15 == 0 && startTime.Second == 0 && startTime.Millisecond == 0;
    }

    private static bool BeValidEndTime(CreateBookingCommand command, DateTime endTime)
    {
        if (endTime == default) return true; // Optional field

        // End time must be after start time
        if (endTime <= command.StartTime) return false;

        // Duration should be reasonable (between 15 minutes and 8 hours)
        var duration = endTime - command.StartTime;
        return duration >= TimeSpan.FromMinutes(15) && duration <= TimeSpan.FromHours(8);
    }

    private static bool BeWithinBusinessHours(DateTime startTime)
    {
        var timeOfDay = startTime.TimeOfDay;
        return timeOfDay >= TimeSpan.FromHours(6) && timeOfDay <= TimeSpan.FromHours(23);
    }
}