using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Domain.Entities;
using System.Globalization;

namespace BookSpot.Infrastructure.Repositories.DynamoDb;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly IDynamoDBContext _context;
    private readonly IAmazonDynamoDB _dynamoDb;

    public PasswordResetTokenRepository(IDynamoDBContext context, IAmazonDynamoDB dynamoDb)
    {
        _context = context;
        _dynamoDb = dynamoDb;
    }

    public async Task<PasswordResetToken?> GetAsync(string token)
    {
        return await _context.LoadAsync<PasswordResetToken>(token, new DynamoDBOperationConfig { ConsistentRead = true });
    }

    public async Task<bool> TryConsumeAsync(string token, string profileId, string passwordHash, int expectedSecurityVersion)
    {
        var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        try
        {
            await _dynamoDb.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems =
                [
                    new TransactWriteItem
                    {
                        Update = new Update
                        {
                            TableName = "profiles",
                            Key = new Dictionary<string, AttributeValue> { ["Id"] = new() { S = profileId } },
                            ConditionExpression = "SecurityVersion = :expected",
                            UpdateExpression = "SET PasswordHash = :hash, SecurityVersion = :next",
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                            {
                                [":expected"] = new() { N = expectedSecurityVersion.ToString(CultureInfo.InvariantCulture) },
                                [":next"] = new() { N = (expectedSecurityVersion + 1).ToString(CultureInfo.InvariantCulture) },
                                [":hash"] = new() { S = passwordHash }
                            }
                        }
                    },
                    new TransactWriteItem
                    {
                        Update = new Update
                        {
                            TableName = "password_reset_tokens",
                            Key = new Dictionary<string, AttributeValue> { ["Token"] = new() { S = token } },
                            ConditionExpression = "(attribute_not_exists(IsUsed) OR IsUsed = :false) AND ExpiresAt > :now",
                            UpdateExpression = "SET IsUsed = :true",
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                            {
                                [":false"] = new() { BOOL = false },
                                [":true"] = new() { BOOL = true },
                                [":now"] = new() { S = now }
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

    public async Task SaveAsync(PasswordResetToken resetToken)
    {
        await _context.SaveAsync(resetToken);
    }

    public async Task DeleteAsync(string token)
    {
        await _context.DeleteAsync<PasswordResetToken>(token);
    }

    public async Task<IEnumerable<PasswordResetToken>> GetByEmailAsync(string email)
    {
        var search = _context.ScanAsync<PasswordResetToken>(new List<ScanCondition>
        {
            new("Email", ScanOperator.Equal, email)
        });
        return await search.GetRemainingAsync();
    }
}
