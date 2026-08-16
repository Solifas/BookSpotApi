using System.Security.Claims;

namespace BookSpot.Application.Abstractions.Services;

public interface IJwtService
{
    string GenerateToken(string userId, string email, string userType, int securityVersion);
    ClaimsPrincipal? ValidateToken(string token);
}