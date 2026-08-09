using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Domain.Entities;

namespace BookSpot.Infrastructure.Repositories.DynamoDb;

public class ProfileRepository : IProfileRepository
{
    private readonly IDynamoDBContext _context;
    public ProfileRepository(IDynamoDBContext context) => _context = context;

    public async Task<Profile?> GetAsync(string id) => await _context.LoadAsync<Profile>(id);

    public async Task<Profile?> GetByEmailAsync(string email)
    {
        var scanConditions = new List<ScanCondition>
        {
            new("Email", ScanOperator.Equal, email)
        };

        var search = _context.ScanAsync<Profile>(scanConditions);
        var profiles = await search.GetNextSetAsync();
        return profiles.FirstOrDefault();
    }

    public Task SaveAsync(Profile profile) => _context.SaveAsync(profile);
    public Task DeleteAsync(string id) => _context.DeleteAsync<Profile>(id);
}