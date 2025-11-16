using Amazon.DynamoDBv2.DataModel;

namespace BookSpot.Domain.Entities;

[DynamoDBTable("password_reset_tokens")]
public class PasswordResetToken
{
    [DynamoDBHashKey]
    public string Token { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Email { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string UserId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public DateTime ExpiresAt { get; set; }

    [DynamoDBProperty]
    public bool IsUsed { get; set; } = false;

    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; }
}