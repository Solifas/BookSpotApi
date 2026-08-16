using BookSpot.Domain.Entities;

namespace BookSpot.Application.DTOs.Auth;

public sealed class ProfileDto
{
    public string ProfileId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? ContactNumber { get; init; }
    public string UserType { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    public static ProfileDto From(Profile profile) => new()
    {
        ProfileId = profile.Id,
        Email = profile.Email,
        FullName = profile.FullName,
        ContactNumber = profile.ContactNumber,
        UserType = profile.UserType,
        CreatedAt = profile.CreatedAt
    };
}

public sealed class AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public DateTime ExpiresAt { get; init; }
    public ProfileDto Profile { get; init; } = new();

    public string Token { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? ContactNumber { get; init; }
    public string UserType { get; init; } = string.Empty;

    public static AuthResponse Create(string accessToken, DateTime expiresAt, Profile profile)
    {
        var dto = ProfileDto.From(profile);
        return new AuthResponse
        {
            AccessToken = accessToken,
            Token = accessToken,
            ExpiresAt = expiresAt,
            Profile = dto,
            UserId = dto.ProfileId,
            Email = dto.Email,
            FullName = dto.FullName,
            ContactNumber = dto.ContactNumber,
            UserType = dto.UserType
        };
    }
}
