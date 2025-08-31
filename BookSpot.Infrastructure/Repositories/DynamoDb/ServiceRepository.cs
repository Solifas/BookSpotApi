using Amazon.DynamoDBv2.DataModel;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Domain.Entities;

namespace BookSpot.Infrastructure.Repositories.DynamoDb;

public class ServiceRepository : IServiceRepository
{
    private readonly IDynamoDBContext _context;
    public ServiceRepository(IDynamoDBContext context) => _context = context;

    public async Task<Service?> GetAsync(string id) => await _context.LoadAsync<Service>(id);
    public Task SaveAsync(Service service) => _context.SaveAsync(service);
    public Task DeleteAsync(string id) => _context.DeleteAsync<Service>(id);
}