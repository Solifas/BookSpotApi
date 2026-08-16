using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Domain.Entities;
using System.Globalization;
using System.Text;

namespace BookSpot.Infrastructure.Repositories.DynamoDb;

public class ProfileRepository : IProfileRepository
{
    private readonly IDynamoDBContext _context;
    private readonly IAmazonDynamoDB _dynamoDb;

    public ProfileRepository(IDynamoDBContext context, IAmazonDynamoDB dynamoDb)
    {
        _context = context;
        _dynamoDb = dynamoDb;
    }

    public async Task<Profile?> GetAsync(string id) => await _context.LoadAsync<Profile>(id);

    public async Task<Profile?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().Normalize().ToLowerInvariant();
        var claim = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = "identity_claims",
            ConsistentRead = true,
            Key = new Dictionary<string, AttributeValue>
            {
                ["ClaimKey"] = new() { S = ClaimKey(normalizedEmail) }
            }
        });
        if (claim.Item.TryGetValue("ProfileId", out var profileId))
        {
            return await _context.LoadAsync<Profile>(profileId.S, new DynamoDBOperationConfig { ConsistentRead = true });
        }

        // Temporary dual-read migration fallback. It must be removed after claims are reconciled.
        var search = _context.ScanAsync<Profile>(new List<ScanCondition>
        {
            new("EmailNormalized", ScanOperator.Equal, normalizedEmail)
        });
        var profiles = await search.GetNextSetAsync();
        if (profiles.Count > 0) return profiles[0];

        var legacy = await _context.ScanAsync<Profile>(new List<ScanCondition>()).GetRemainingAsync();
        return legacy.FirstOrDefault(profile =>
            string.Equals(profile.Email.Trim().Normalize().ToLowerInvariant(), normalizedEmail, StringComparison.Ordinal));
    }

    public async Task<bool> CreateAsync(Profile profile)
    {
        var now = profile.CreatedAt.ToString("O", CultureInfo.InvariantCulture);
        try
        {
            await _dynamoDb.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems =
                [
                    new TransactWriteItem
                    {
                        Put = new Put
                        {
                            TableName = "identity_claims",
                            ConditionExpression = "attribute_not_exists(ClaimKey)",
                            Item = new Dictionary<string, AttributeValue>
                            {
                                ["ClaimKey"] = new() { S = ClaimKey(profile.EmailNormalized) },
                                ["Kind"] = new() { S = "NORMALIZED_EMAIL" },
                                ["ProfileId"] = new() { S = profile.Id },
                                ["EmailNormalized"] = new() { S = profile.EmailNormalized },
                                ["CreatedAtUtc"] = new() { S = now },
                                ["SchemaVersion"] = new() { N = "1" }
                            }
                        }
                    },
                    new TransactWriteItem
                    {
                        Put = new Put
                        {
                            TableName = "profiles",
                            ConditionExpression = "attribute_not_exists(Id)",
                            Item = new Dictionary<string, AttributeValue>
                            {
                                ["Id"] = new() { S = profile.Id },
                                ["Email"] = new() { S = profile.Email },
                                ["EmailNormalized"] = new() { S = profile.EmailNormalized },
                                ["FullName"] = new() { S = profile.FullName },
                                ["ContactNumber"] = profile.ContactNumber is null ? new() { NULL = true } : new() { S = profile.ContactNumber },
                                ["UserType"] = new() { S = profile.UserType },
                                ["PasswordHash"] = new() { S = profile.PasswordHash },
                                ["SecurityVersion"] = new() { N = profile.SecurityVersion.ToString(CultureInfo.InvariantCulture) },
                                ["CreatedAt"] = new() { S = now }
                            }
                        }
                    }
                ]
            });
            return true;
        }
        catch (TransactionCanceledException)
        {
            return false;
        }
    }

    public Task SaveAsync(Profile profile) => _context.SaveAsync(profile);
    public Task DeleteAsync(string id) => _context.DeleteAsync<Profile>(id);

    private static string ClaimKey(string normalizedEmail) =>
        $"EMAIL#v1#{Convert.ToBase64String(Encoding.UTF8.GetBytes(normalizedEmail)).TrimEnd('=').Replace('+', '-').Replace('/', '_')}";
}
