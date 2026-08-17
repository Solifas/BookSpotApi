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
    public string BusinessId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string ClientId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string ProviderId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string ProviderProfileId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string ProviderName { get; set; } = string.Empty;

    [DynamoDBProperty]
    public DateTime StartTime { get; set; }

    [DynamoDBProperty]
    public DateTime EndTime { get; set; }

    [DynamoDBProperty]
    public string Status { get; set; } = "pending";

    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; }

    [DynamoDBProperty]
    public DateTime UpdatedAt { get; set; }

    [DynamoDBProperty]
    public int Version { get; set; } = 1;

    [DynamoDBProperty]
    public decimal? PriceAmount { get; set; }

    [DynamoDBProperty]
    public string? ServiceNameSnapshot { get; set; }

    [DynamoDBProperty]
    public int? DurationMinutesSnapshot { get; set; }

    [DynamoDBProperty]
    public string? BusinessNameSnapshot { get; set; }

    [DynamoDBProperty]
    public string? BusinessAddressSnapshot { get; set; }

    [DynamoDBProperty]
    public string? BusinessCitySnapshot { get; set; }

    [DynamoDBProperty]
    public string? ClientFullNameSnapshot { get; set; }

    [DynamoDBProperty]
    public string? ClientEmailSnapshot { get; set; }

    [DynamoDBProperty]
    public string? ClientPhoneSnapshot { get; set; }
}
