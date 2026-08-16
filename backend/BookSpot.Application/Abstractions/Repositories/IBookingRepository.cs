using BookSpot.Domain.Entities;

namespace BookSpot.Application.Abstractions.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetAsync(string id);
    Task<Booking> CreateAsync(Booking booking, string idempotencyKey, string requestFingerprint);
    Task<Booking> ApplyActionAsync(BookingActionPersistenceRequest request);
    Task SaveAsync(Booking booking);
    Task DeleteAsync(string id);
    Task<IEnumerable<Booking>> GetConflictingBookingsAsync(string providerId, DateTime startTime, DateTime endTime);
    Task<IEnumerable<Booking>> GetBookingsByProviderAsync(string providerId);
    Task<IEnumerable<Booking>> GetBookingsByClientAsync(string clientId);
}

public sealed record BookingActionPersistenceRequest(
    Booking Booking,
    string Action,
    string SourceStatus,
    int SourceVersion,
    DateTime OldStartTime,
    DateTime OldEndTime,
    string ActorProfileId,
    string ActorRole,
    string IdempotencyKey,
    string RequestFingerprint);