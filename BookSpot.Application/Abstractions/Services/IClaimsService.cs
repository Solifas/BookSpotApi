using System.Security.Claims;

namespace BookSpot.Application.Abstractions.Services;

public interface IClaimsService
{
    string? GetCurrentUserId();
    string? GetCurrentUserEmail();
    string? GetCurrentUserType();
    string? GetCurrentUserRole();
    bool IsAuthenticated();
    bool IsInRole(string role);
    bool IsProvider();
    bool IsClient();
    IEnumerable<Claim> GetAllClaims();
}