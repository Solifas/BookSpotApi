using Amazon.DynamoDBv2.DataModel;

namespace BookSpot.Domain.Entities;

[DynamoDBTable("business_hours")]
public class BusinessHour
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string BusinessId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public int DayOfWeek { get; set; }

    [DynamoDBProperty]
    public string OpenTime { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string CloseTime { get; set; } = string.Empty;

    [DynamoDBProperty]
    public bool IsClosed { get; set; }
}