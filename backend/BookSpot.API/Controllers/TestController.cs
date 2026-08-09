using BookSpot.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

[ApiController]
[Route("test")]
public class TestController : ControllerBase
{
    /// <summary>
    /// Test endpoint to demonstrate exception handling
    /// </summary>
    [HttpGet("exception/{type}")]
    public IActionResult TestException(string type)
    {
        return type.ToLower() switch
        {
            "notfound" => throw new NotFoundException("Test resource", "123"),
            "validation" => throw new ValidationException("Test validation error"),
            "argument" => throw new ArgumentException("Test argument exception"),
            "server" => throw new InvalidOperationException("Test server error"),
            _ => Ok(new { message = "No exception thrown", availableTypes = new[] { "notfound", "validation", "argument", "server" } })
        };
    }

    /// <summary>
    /// Test endpoint to demonstrate validation exception with detailed errors
    /// </summary>
    [HttpGet("validation-details")]
    public IActionResult TestValidationWithDetails()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "Name", new[] { "Name is required", "Name must be at least 3 characters" } },
            { "Email", new[] { "Email is required", "Email format is invalid" } },
            { "Age", new[] { "Age must be between 18 and 100" } }
        };

        throw new ValidationException(errors);
    }
}