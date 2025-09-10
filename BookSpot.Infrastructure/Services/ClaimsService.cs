using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using BookSpot.Application.Abstractions.Services;

namespace BookSpot.Infrastructure.Services;

public class ClaimsService : IClaimsService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public string? GetCurrentUserEmail()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
    }

    public string? GetCurrentUserType()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("user_type")?.Value;
    }

    public string? GetCurrentUserRole()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }

    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }

    public bool IsProvider()
    {
        return GetCurrentUserType() == "provider";
    }

    public bool IsClient()
    {
        return GetCurrentUserType() == "client";
    }

    public IEnumerable<Claim> GetAllClaims()
    {
        return _httpContextAccessor.HttpContext?.User?.Claims ?? Enumerable.Empty<Claim>();
    }
}