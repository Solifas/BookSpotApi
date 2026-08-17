using BookSpot.Domain.Entities;

namespace BookSpot.Application.Abstractions.Repositories;

public interface IReviewRepository
{
    Task<Review?> GetAsync(string id);
    Task<Review?> GetByBookingAsync(string bookingId);
    Task<bool> CreateAsync(Review review);
    Task SaveAsync(Review review);
    Task DeleteAsync(string id);
}