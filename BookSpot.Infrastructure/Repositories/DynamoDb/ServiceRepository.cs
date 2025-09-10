using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;

namespace BookSpot.Infrastructure.Repositories.DynamoDb;

public class ServiceRepository : IServiceRepository
{
    private readonly IDynamoDBContext _context;
    public ServiceRepository(IDynamoDBContext context) => _context = context;

    public async Task<Service?> GetAsync(string id)
    {
        try
        {
            return await _context.LoadAsync<Service>(id);
        }
        catch (ResourceNotFoundException)
        {
            return null;
        }
        catch (Exception ex) when (IsDynamoDbException(ex))
        {
            throw new InvalidOperationException("Failed to retrieve service from database", ex);
        }
    }

    public async Task<IEnumerable<Service>> GetAllAsync()
    {
        try
        {
            var search = _context.ScanAsync<Service>(new List<ScanCondition>());
            return await search.GetRemainingAsync();
        }
        catch (ResourceNotFoundException)
        {
            // If table doesn't exist, return empty collection instead of throwing
            return new List<Service>();
        }
        catch (Exception ex) when (IsDynamoDbException(ex))
        {
            throw new InvalidOperationException("Failed to retrieve services from database", ex);
        }
    }

    public async Task SaveAsync(Service service)
    {
        try
        {
            await _context.SaveAsync(service);
        }
        catch (ResourceNotFoundException)
        {
            throw new InvalidOperationException("Database table not found. Please ensure the database is properly configured.");
        }
        catch (Exception ex) when (IsDynamoDbException(ex))
        {
            throw new InvalidOperationException("Failed to save service to database", ex);
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            await _context.DeleteAsync<Service>(id);
        }
        catch (ResourceNotFoundException)
        {
            // If the item or table doesn't exist, consider it already deleted
            return;
        }
        catch (Exception ex) when (IsDynamoDbException(ex))
        {
            throw new InvalidOperationException("Failed to delete service from database", ex);
        }
    }

    private static bool IsDynamoDbException(Exception ex)
    {
        return ex is AmazonDynamoDBException ||
               ex.GetType().FullName?.Contains("DynamoDB") == true ||
               ex.GetType().FullName?.Contains("Amazon") == true;
    }
}