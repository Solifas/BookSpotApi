using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Domain.Entities;

namespace BookSpot.Infrastructure.Repositories.DynamoDb;

public class ReviewRepository : IReviewRepository
{
    private readonly IDynamoDBContext _context;
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;
    public ReviewRepository(IDynamoDBContext context, IAmazonDynamoDB dynamoDb,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _context = context;
        _dynamoDb = dynamoDb;
        _tableName = configuration["DynamoDB:Tables:Reviews"] ?? "reviews";
    }

    public async Task<Review?> GetAsync(string id) => await _context.LoadAsync<Review>(id);
    public async Task<Review?> GetByBookingAsync(string bookingId)
    {
        var search = _context.ScanAsync<Review>(
            new[] { new ScanCondition(nameof(Review.BookingId), ScanOperator.Equal, bookingId) });
        return (await search.GetRemainingAsync()).SingleOrDefault();
    }
    public async Task<bool> CreateAsync(Review review)
    {
        try
        {
            await _dynamoDb.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    ["Id"] = new(review.Id),
                    ["BookingId"] = new(review.BookingId),
                    ["Rating"] = new() { N = review.Rating.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                    ["Comment"] = new(review.Comment),
                    ["CreatedAt"] = new(review.CreatedAt.ToUniversalTime().ToString("O"))
                },
                ConditionExpression = "attribute_not_exists(Id)"
            });
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }
    public Task SaveAsync(Review review) => _context.SaveAsync(review);
    public Task DeleteAsync(string id) => _context.DeleteAsync<Review>(id);
}