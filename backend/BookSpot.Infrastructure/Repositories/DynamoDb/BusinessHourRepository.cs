using Amazon.DynamoDBv2.DataModel;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Domain.Entities;

namespace BookSpot.Infrastructure.Repositories.DynamoDb;

public class BusinessHourRepository : IBusinessHourRepository
{
    private readonly IDynamoDBContext _context;
    public BusinessHourRepository(IDynamoDBContext context) => _context = context;

    public async Task<BusinessHour?> GetAsync(string id) => await _context.LoadAsync<BusinessHour>(id);
    public Task SaveAsync(BusinessHour businessHour) => _context.SaveAsync(businessHour);
    public Task DeleteAsync(string id) => _context.DeleteAsync<BusinessHour>(id);
}