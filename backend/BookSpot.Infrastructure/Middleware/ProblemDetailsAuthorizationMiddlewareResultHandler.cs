using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.Infrastructure.Middleware;

public sealed class ProblemDetailsAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private static readonly AuthorizationMiddlewareResultHandler DefaultHandler = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await DefaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        var forbidden = authorizeResult.Forbidden;
        var status = forbidden ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized;
        var code = forbidden ? "role_forbidden" : "authentication_required";
        var problem = new ProblemDetails
        {
            Type = $"https://bookspot.example/problems/{code.Replace('_', '-')}",
            Title = forbidden ? "Role forbidden" : "Authentication required",
            Status = status,
            Detail = forbidden
                ? "The authenticated role cannot access this resource."
                : "Authentication is required to access this resource.",
            Instance = SafeInstance(context)
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(context.Response.Body, problem, JsonOptions, context.RequestAborted);
    }

    private static string SafeInstance(HttpContext context)
    {
        var route = (context.GetEndpoint() as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern.RawText;
        return string.IsNullOrWhiteSpace(route) ? "/" : $"/{route.TrimStart('/')}";
    }
}
