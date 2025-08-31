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
}