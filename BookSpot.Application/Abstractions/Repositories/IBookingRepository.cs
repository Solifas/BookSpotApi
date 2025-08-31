using BookSpot.Domain.Entities;

namespace BookSpot.Application.Abstractions.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetAsync(string id);
    Task SaveAsync(Booking booking);
    Task DeleteAsync(string id);
}