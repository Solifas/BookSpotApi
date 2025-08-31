using Amazon.DynamoDBv2.DataModel;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Domain.Entities;

namespace BookSpot.Infrastructure.Repositories.DynamoDb;

public class ProfileRepository : IProfileRepository
{
    private readonly IDynamoDBContext _context;
    public ProfileRepository(IDynamoDBContext context) => _context = context;

    public async Task<Profile?> GetAsync(string id) => await _context.LoadAsync<Profile>(id);
    public Task SaveAsync(Profile profile) => _context.SaveAsync(profile);
    public Task DeleteAsync(string id) => _context.DeleteAsync<Profile>(id);
}