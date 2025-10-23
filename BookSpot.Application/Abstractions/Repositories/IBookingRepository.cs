using BookSpot.Domain.Entities;

namespace BookSpot.Application.Abstractions.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetAsync(string id);
    Task SaveAsync(Booking booking);
    Task DeleteAsync(string id);
    Task<IEnumerable<Booking>> GetConflictingBookingsAsync(string providerId, DateTime startTime, DateTime endTime);
    Task<IEnumerable<Booking>> GetBookingsByProviderAsync(string providerId);
    Task<IEnumerable<Booking>> GetBookingsByClientAsync(string clientId);
}