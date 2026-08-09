using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Domain.Entities;

namespace BookSpot.Infrastructure.Repositories.DynamoDb;

public class BusinessRepository : IBusinessRepository
{
    private readonly IDynamoDBContext _context;
    public BusinessRepository(IDynamoDBContext context) => _context = context;

    public async Task<Business?> GetAsync(string id) => await _context.LoadAsync<Business>(id);
    public Task SaveAsync(Business business) => _context.SaveAsync(business);
    public Task DeleteAsync(string id) => _context.DeleteAsync<Business>(id);

    public async Task<IEnumerable<Business>> GetAllAsync()
    {
        var search = _context.ScanAsync<Business>(new List<ScanCondition>());
        return await search.GetRemainingAsync();
    }
}