namespace BookSpot.Application.DTOs.Bookings;

public sealed record BookingActionRequest(
    string Action,
    int ExpectedVersion,
    DateTimeOffset? StartTime);
