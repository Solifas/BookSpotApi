using BookSpot.Domain.Entities;

namespace BookSpot.Application.Abstractions.Repositories;

public interface IReviewRepository
{
    Task<Review?> GetAsync(string id);
    Task SaveAsync(Review review);
    Task DeleteAsync(string id);
}