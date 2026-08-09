using Amazon.DynamoDBv2.DataModel;

namespace BookSpot.Domain.Entities;

[DynamoDBTable("services")]
public class Service
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string BusinessId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string ProviderId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string ProviderName { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Name { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Description { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string? Category { get; set; }

    [DynamoDBProperty]
    public decimal Price { get; set; }

    [DynamoDBProperty]
    public int DurationMinutes { get; set; }

    [DynamoDBProperty]
    public string? ImageUrl { get; set; }

    [DynamoDBProperty]
    public List<string> Tags { get; set; } = [];

    [DynamoDBProperty]
    public string? Location { get; set; }

    [DynamoDBProperty]
    public bool IsActive { get; set; } = true;

    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; }
}
