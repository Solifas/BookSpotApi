using System.Net;
using System.Text.Json;
using BookSpot.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookSpot.Infrastructure.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError("Unhandled {ExceptionType} (trace {TraceId})",
            exception.GetType().Name, httpContext.TraceIdentifier);

        var problemDetails = CreateProblemDetails(httpContext, exception);
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, problemDetails, JsonOptions, cancellationToken);
        return true;
    }

    private static ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var (status, title, detail, code) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed",
                "One or more request fields are invalid.", "validation_failed"),
            BadRequestException or ArgumentException or InvalidOperationException =>
                (StatusCodes.Status400BadRequest, "Invalid request", "The request is invalid.", "invalid_request"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Authentication required",
                "Authentication is required to access this resource.", "authentication_required"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found",
                "The requested resource was not found.", "resource_not_found"),
            ConflictException conflict => (StatusCodes.Status409Conflict, "Conflict",
                SafeConflictDetail(conflict), ConflictCode(conflict)),
            TimeoutException => (StatusCodes.Status503ServiceUnavailable, "Persistence unavailable",
                "The service is temporarily unavailable.", "persistence_unavailable"),
            NotImplementedException => ((int)HttpStatusCode.NotImplemented, "Not implemented",
                "The requested capability is not implemented.", "not_implemented"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error",
                "An unexpected error occurred.", "internal_server_error")
        };

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = SafeInstance(context),
            Type = $"https://bookspot.example/problems/{code.Replace('_', '-')}"
        };
        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        if (exception is ValidationException validationException && validationException.Errors.Any())
            problemDetails.Extensions["errors"] = validationException.Errors;
        return problemDetails;
    }

    private static string SafeInstance(HttpContext context)
    {
        var route = (context.GetEndpoint() as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern.RawText;
        return string.IsNullOrWhiteSpace(route) ? "/" : $"/{route.TrimStart('/')}";
    }

    private static string ConflictCode(ConflictException exception) => exception.Code;

    private static string SafeConflictDetail(ConflictException exception) => ConflictCode(exception) switch
    {
        "booking_slot_conflict" => "The requested booking slot is no longer available.",
        "idempotency_key_reused" => "The idempotency key was already used for a different request.",
        _ => "The requested operation conflicts with the current resource state."
    };
}
