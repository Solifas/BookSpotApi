using System.Text.Json;
using BookSpot.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace BookSpot.Tests;

public sealed class AuthorizationProblemDetailsTests
{
    [Theory]
    [InlineData(false, 401, "authentication_required")]
    [InlineData(true, 403, "role_forbidden")]
    public async Task AuthorizationFailures_UseStableProblemDetails(bool forbidden, int status, string code)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/dashboard/me";
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "trace-auth";
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var result = forbidden ? PolicyAuthorizationResult.Forbid() : PolicyAuthorizationResult.Challenge();

        await new ProblemDetailsAuthorizationMiddlewareResultHandler().HandleAsync(
            _ => Task.CompletedTask, context, policy, result);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(status, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
        Assert.Equal("trace-auth", document.RootElement.GetProperty("traceId").GetString());
    }
}
