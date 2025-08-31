using Amazon.DynamoDBv2.DataModel;

namespace BookSpot.Domain.Entities;

[DynamoDBTable("bookings")]
public class Booking
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string ServiceId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string ClientId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string ProviderId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public DateTime StartTime { get; set; }

    [DynamoDBProperty]
    public DateTime EndTime { get; set; }

    [DynamoDBProperty]
    public string Status { get; set; } = "pending";

    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; }
}