using BookSpot.Domain.Entities;

namespace BookSpot.Application.Abstractions.Repositories;

public interface IServiceRepository
{
    Task<Service?> GetAsync(string id);
    Task SaveAsync(Service service);
    Task DeleteAsync(string id);
}