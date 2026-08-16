using BookSpot.Application.DTOs.Auth;
using BookSpot.Application.Features.Auth;

namespace BookSpot.Tests;

public class AuthContractTests
{
    [Fact]
    public void AuthResponse_FromProfile_EmitsCanonicalAndCompatibilityFields()
    {
        var expires = new DateTime(2026, 8, 13, 12, 15, 0, DateTimeKind.Utc);
        var response = AuthResponse.Create("jwt", expires, new Domain.Entities.Profile
        {
            Id = "profile-1",
            Email = "user@example.com",
            FullName = "User Name",
            UserType = "client",
            CreatedAt = expires.AddDays(-1)
        });

        Assert.Equal("jwt", response.AccessToken);
        Assert.Equal("jwt", response.Token);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal(response.Profile.ProfileId, response.UserId);
        Assert.Equal(response.Profile.Email, response.Email);
        Assert.Equal(response.Profile.UserType, response.UserType);
    }

    [Fact]
    public void ProfileDto_DoesNotExposePasswordHash()
    {
        var properties = typeof(ProfileDto).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain(properties, x => x.Contains("Password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, x => x.Contains("Hash", StringComparison.OrdinalIgnoreCase));
    }
}
