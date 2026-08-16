namespace BookSpot.Application.DTOs.Bookings;

public sealed record CreateBookingRequest(string ServiceId, DateTimeOffset StartTime);

public sealed record BookingMutationResultDto(
    string View,
    string BookingId,
    string Status,
    DateTime StartTime,
    DateTime EndTime,
    int Version,
    DateTime UpdatedAt)
{
    public static BookingMutationResultDto From(BookSpot.Domain.Entities.Booking booking, string view) =>
        new(view, booking.Id, booking.Status, booking.StartTime, booking.EndTime, booking.Version, booking.UpdatedAt);
}