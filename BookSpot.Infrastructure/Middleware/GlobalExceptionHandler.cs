using System.Net;
using BookSpot.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookSpot.Infrastructure.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
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
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var problemDetails = CreateProblemDetails(httpContext, exception);

        httpContext.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCode(exception);
        var title = GetTitle(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path,
            Type = GetTypeUrl(statusCode)
        };

        // Add validation errors for ValidationException
        if (exception is ValidationException validationException && validationException.Errors.Any())
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        return problemDetails;
    }

    private static int GetStatusCode(Exception exception) => exception switch
    {
        NotFoundException => (int)HttpStatusCode.NotFound,
        ValidationException => (int)HttpStatusCode.BadRequest,
        BadRequestException => (int)HttpStatusCode.BadRequest,
        ArgumentException => (int)HttpStatusCode.BadRequest,
        InvalidOperationException => (int)HttpStatusCode.BadRequest,
        UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
        NotImplementedException => (int)HttpStatusCode.NotImplemented,
        TimeoutException => (int)HttpStatusCode.RequestTimeout,
        _ => (int)HttpStatusCode.InternalServerError
    };

    private static string GetTitle(Exception exception) => exception switch
    {
        NotFoundException => "Not Found",
        ValidationException => "Validation Error",
        BadRequestException => "Bad Request",
        ArgumentException => "Bad Request",
        InvalidOperationException => "Bad Request",
        UnauthorizedAccessException => "Unauthorized",
        NotImplementedException => "Not Implemented",
        TimeoutException => "Request Timeout",
        _ => "Internal Server Error"
    };

    private static string GetTypeUrl(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
        404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        408 => "https://tools.ietf.org/html/rfc7231#section-6.5.7",
        500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        501 => "https://tools.ietf.org/html/rfc7231#section-6.6.2",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
    };
}