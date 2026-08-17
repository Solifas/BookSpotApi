using FluentValidation;

namespace BookSpot.Application.Features.BusinessHours.Commands;

public class CreateBusinessHourCommandValidator : AbstractValidator<CreateBusinessHourCommand>
{
    public CreateBusinessHourCommandValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty()
            .WithMessage("Business ID is required.")
            .Must(BeValidOpaqueId)
            .WithMessage("Business ID must be 1-128 UTF-8 bytes without control characters.");

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

    private static bool BeValidOpaqueId(string id) =>
        System.Text.Encoding.UTF8.GetByteCount(id) <= 128 && id.All(character => !char.IsControl(character));

    private static bool BeValidTimeFormat(string time)
    {
        return TimeOnly.TryParseExact(time, "HH:mm", System.Globalization.CultureInfo.InvariantCulture,
                   System.Globalization.DateTimeStyles.None, out var parsed) && parsed.Minute % 15 == 0;
    }

    private static bool HaveValidTimeRange(CreateBusinessHourCommand command)
    {
        if (command.IsClosed || string.IsNullOrEmpty(command.OpenTime) || string.IsNullOrEmpty(command.CloseTime))
            return true;

        if (TimeSpan.TryParse(command.OpenTime, out var openTime) &&
            TimeSpan.TryParse(command.CloseTime, out var closeTime))
        {
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
            var duration = closeTime - openTime;
            return duration <= TimeSpan.FromHours(16);
        }

        return false;
    }
}