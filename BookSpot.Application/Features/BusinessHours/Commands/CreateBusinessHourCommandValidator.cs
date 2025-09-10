using FluentValidation;

namespace BookSpot.Application.Features.BusinessHours.Commands;

public class CreateBusinessHourCommandValidator : AbstractValidator<CreateBusinessHourCommand>
{
    public CreateBusinessHourCommandValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty()
            .WithMessage("Business ID is required.")
            .Must(BeValidGuid)
            .WithMessage("Business ID must be a valid GUID format.");

        RuleFor(x => x.DayOfWeek)
            .InclusiveBetween(0, 6)
            .WithMessage("Day of week must be between 0 (Sunday) and 6 (Saturday).");

        RuleFor(x => x.OpenTime)
            .NotEmpty()
            .When(x => !x.IsClosed)
            .WithMessage("Open time is required when business is not closed.")
            .Must(BeValidTimeFormat)
            .When(x => !x.IsClosed && !string.IsNullOrEmpty(x.OpenTime))
            .WithMessage("Open time must be in valid time format (HH:mm).");

        RuleFor(x => x.CloseTime)
            .NotEmpty()
            .When(x => !x.IsClosed)
            .WithMessage("Close time is required when business is not closed.")
            .Must(BeValidTimeFormat)
            .When(x => !x.IsClosed && !string.IsNullOrEmpty(x.CloseTime))
            .WithMessage("Close time must be in valid time format (HH:mm).");

        RuleFor(x => x)
            .Must(HaveValidTimeRange)
            .When(x => !x.IsClosed)
            .WithMessage("Close time must be after open time.");

        RuleFor(x => x)
            .Must(HaveReasonableHours)
            .When(x => !x.IsClosed)
            .WithMessage("Business hours cannot exceed 16 hours per day.");
    }

    private static bool BeValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }

    private static bool BeValidTimeFormat(string time)
    {
        return TimeSpan.TryParse(time, out _);
    }

    private static bool HaveValidTimeRange(CreateBusinessHourCommand command)
    {
        if (command.IsClosed || string.IsNullOrEmpty(command.OpenTime) || string.IsNullOrEmpty(command.CloseTime))
            return true;

        if (TimeSpan.TryParse(command.OpenTime, out var openTime) &&
            TimeSpan.TryParse(command.CloseTime, out var closeTime))
        {
            // Handle overnight hours (e.g., 22:00 to 02:00)
            if (closeTime < openTime)
            {
                closeTime = closeTime.Add(TimeSpan.FromDays(1));
            }

            return closeTime > openTime;
        }

        return false;
    }

    private static bool HaveReasonableHours(CreateBusinessHourCommand command)
    {
        if (command.IsClosed || string.IsNullOrEmpty(command.OpenTime) || string.IsNullOrEmpty(command.CloseTime))
            return true;

        if (TimeSpan.TryParse(command.OpenTime, out var openTime) &&
            TimeSpan.TryParse(command.CloseTime, out var closeTime))
        {
            // Handle overnight hours
            if (closeTime < openTime)
            {
                closeTime = closeTime.Add(TimeSpan.FromDays(1));
            }

            var duration = closeTime - openTime;
            return duration <= TimeSpan.FromHours(16);
        }

        return false;
    }
}