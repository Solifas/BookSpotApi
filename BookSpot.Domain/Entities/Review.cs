using Amazon.DynamoDBv2.DataModel;

namespace BookSpot.Domain.Entities;

[DynamoDBTable("reviews")]
public class Review
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string BookingId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public int Rating { get; set; }

    [DynamoDBProperty]
    public string Comment { get; set; } = string.Empty;
}