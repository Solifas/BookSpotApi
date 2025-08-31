using BookSpot.Domain.Entities;

namespace BookSpot.Application.Abstractions.Repositories;

public interface IBusinessRepository
{
    Task<Business?> GetAsync(string id);
    Task SaveAsync(Business business);
    Task DeleteAsync(string id);
}