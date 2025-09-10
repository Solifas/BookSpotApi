using Amazon.DynamoDBv2.DataModel;

namespace BookSpot.Domain.Entities;

[DynamoDBTable("profiles")]
public class Profile
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Email { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string UserType { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string PasswordHash { get; set; } = string.Empty;

    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; }
}