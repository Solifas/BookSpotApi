using BookSpot.Domain.Entities;

namespace BookSpot.Application.Abstractions.Repositories;

public interface IBusinessHourRepository
{
    Task<BusinessHour?> GetAsync(string id);
    Task SaveAsync(BusinessHour businessHour);
    Task DeleteAsync(string id);
}