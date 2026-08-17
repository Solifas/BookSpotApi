using BookSpot.Application.DTOs.Canonical;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;

namespace BookSpot.Application.Features.Availability;

public static class AvailabilityCalculator
{
    public static ServiceAvailabilityDto Calculate(Service service, IEnumerable<BusinessHour> schedule,
        IEnumerable<Booking> bookings, DateTimeOffset from, DateTimeOffset to, string timeZone = CanonicalDtoMapper.DefaultTimeZone)
    {
        if (to <= from || to - from > TimeSpan.FromDays(31))
        {
            throw new ValidationException("Availability range must be positive and no longer than 31 days.");
        }
        if (service.DurationMinutes is < 15 or > 480 || service.DurationMinutes % 15 != 0)
        {
            throw new ValidationException("Service duration must be between 15 and 480 minutes on a 15-minute grid.");
        }

        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone); }
        catch (TimeZoneNotFoundException) { throw new ValidationException("Business timezone is invalid."); }
        catch (InvalidTimeZoneException) { throw new ValidationException("Business timezone is invalid."); }

        var liveBookings = bookings.Where(value => value.Status is "pending" or "confirmed").ToArray();
        var hoursByDay = schedule.Where(value => !value.IsClosed).GroupBy(value => value.DayOfWeek)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var slots = new List<AvailabilitySlotDto>();
        var localStartDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(from, zone).Date);
        var localEndDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(to.AddTicks(-1), zone).Date);

        for (var date = localStartDate; date <= localEndDate; date = date.AddDays(1))
        {
            if (!hoursByDay.TryGetValue((int)date.DayOfWeek, out var dayHours)) continue;
            foreach (var hours in dayHours)
            {
                if (!TimeOnly.TryParseExact(hours.OpenTime, "HH:mm", out var open) ||
                    !TimeOnly.TryParseExact(hours.CloseTime, "HH:mm", out var close) || open >= close) continue;

                var local = date.ToDateTime(open, DateTimeKind.Unspecified);
                var localClose = date.ToDateTime(close, DateTimeKind.Unspecified);
                while (local.AddMinutes(service.DurationMinutes) <= localClose)
                {
                    if (!zone.IsInvalidTime(local) && !zone.IsAmbiguousTime(local))
                    {
                        var startUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
                        var endUtc = startUtc.AddMinutes(service.DurationMinutes);
                        if (startUtc >= from && endUtc <= to && !liveBookings.Any(booking =>
                            booking.StartTime < endUtc.UtcDateTime && booking.EndTime > startUtc.UtcDateTime))
                        {
                            slots.Add(new AvailabilitySlotDto(startUtc, endUtc));
                        }
                    }
                    local = local.AddMinutes(15);
                }
            }
        }

        return new ServiceAvailabilityDto(service.Id, service.BusinessId, timeZone, from.ToUniversalTime(),
            to.ToUniversalTime(), service.DurationMinutes, slots.OrderBy(value => value.StartTime).ToArray());
    }
}
