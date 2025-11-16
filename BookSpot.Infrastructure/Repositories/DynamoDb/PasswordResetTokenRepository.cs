using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Domain.Entities;

namespace BookSpot.Infrastructure.Repositories.DynamoDb;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly IDynamoDBContext _context;

    public PasswordResetTokenRepository(IDynamoDBContext context)
    {
        _context = context;
    }

    public async Task<PasswordResetToken?> GetAsync(string token)
    {
        return await _context.LoadAsync<PasswordResetToken>(token);
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
        var config = new DynamoDBOperationConfig
        {
            IndexName = "EmailIndex",
            QueryFilter = new List<ScanCondition>
            {
                new("Email", ScanOperator.Equal, email)
            }
        };

        var search = _context.ScanAsync<PasswordResetToken>(new List<ScanCondition>
        {
            new("Email", ScanOperator.Equal, email)
        });

        return await search.GetRemainingAsync();
    }
}