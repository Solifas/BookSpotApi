using BookSpot.Domain.Entities;

namespace BookSpot.Application.Abstractions.Repositories;

public interface IProfileRepository
{
    Task<Profile?> GetAsync(string id);
    Task<Profile?> GetByEmailAsync(string email);
    Task SaveAsync(Profile profile);
    Task DeleteAsync(string id);
}