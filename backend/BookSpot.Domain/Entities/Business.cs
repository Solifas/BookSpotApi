using Amazon.DynamoDBv2.DataModel;

namespace BookSpot.Domain.Entities;

[DynamoDBTable("businesses")]
public class Business
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string ProviderId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string BusinessName { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string City { get; set; } = string.Empty;

    [DynamoDBProperty]
    public bool IsActive { get; set; } = true;

    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; }

    [DynamoDBProperty]
    public string Description { get; set; } = string.Empty;
    [DynamoDBProperty]
    public string Address { get; set; } = string.Empty;
    [DynamoDBProperty]
    public string Phone { get; set; } = string.Empty;
    [DynamoDBProperty]
    public string Email { get; set; } = string.Empty;
    [DynamoDBProperty]
    public string? Website { get; set; }
    [DynamoDBProperty]
    public string? ImageUrl { get; set; }
    [DynamoDBProperty]
    public double Rating { get; set; } = 0.0;
    [DynamoDBProperty]
    public int ReviewCount { get; set; } = 0;
}