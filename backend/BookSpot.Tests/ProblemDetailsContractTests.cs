using System.Text.Json;
using BookSpot.Application.Exceptions;
using BookSpot.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookSpot.Tests;

public sealed class ProblemDetailsContractTests
{
    [Fact]
    public async Task ValidationException_UsesStableProblemDetailsContract()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/services/service-1";
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "trace-1";
        var exception = new ValidationException(new Dictionary<string, string[]>
        {
            ["$"] = ["empty_patch"]
        });

        await new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance)
            .TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var root = document.RootElement;
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal("https://bookspot.example/problems/validation-failed", root.GetProperty("type").GetString());
        Assert.Equal("Validation failed", root.GetProperty("title").GetString());
        Assert.Equal("validation_failed", root.GetProperty("code").GetString());
        Assert.Equal("trace-1", root.GetProperty("traceId").GetString());
        Assert.Equal("One or more request fields are invalid.", root.GetProperty("detail").GetString());
        Assert.Equal("empty_patch", root.GetProperty("errors").GetProperty("$")[0].GetString());
    }

    [Fact]
    public async Task NotFoundException_DoesNotLeakResourceKey()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/businesses/private-id";
        context.Response.Body = new MemoryStream();
        var exception = new NotFoundException("Business", "private-id");

        await new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance)
            .TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var root = document.RootElement;
        Assert.Equal("resource_not_found", root.GetProperty("code").GetString());
        Assert.Equal("The requested resource was not found.", root.GetProperty("detail").GetString());
        Assert.DoesNotContain("private-id", root.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task BookingSlotConflict_UsesSpecificStableCode()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/bookings";
        context.Response.Body = new MemoryStream();
        var exception = new ConflictException(
            "The requested booking slot is no longer available.", "booking_slot_conflict");

        await new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance)
            .TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("booking_slot_conflict", document.RootElement.GetProperty("code").GetString());
    }
}
