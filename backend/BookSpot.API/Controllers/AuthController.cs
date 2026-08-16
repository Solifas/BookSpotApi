using BookSpot.Application.DTOs.Auth;
using BookSpot.Application.Features.Auth.Commands;
using BookSpot.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

/// <summary>
/// Authentication controller for user authentication and password management
/// </summary>
[ApiController]
[Route("auth")]
[Produces("application/json")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        SetNoStore();
        var response = await _mediator.Send(new RegisterCommand(
            request.Email,
            request.FullName,
            request.ContactNumber,
            request.Password,
            request.UserType));

        return Created("/profiles/me", response);
    }

    /// <summary>
    /// Authenticate user and get JWT token
    /// <param name="request">Login credentials (email and password)</param>
    /// <returns>Authentication response with JWT token and user information</returns>
    /// <response code="200">User successfully authenticated</response>
    /// <response code="400">Invalid credentials or validation errors</response>
    /// <response code="401">Authentication failed</response>
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        SetNoStore();
        var command = new LoginCommand(request.Email, request.Password);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Request password reset for a user
    /// </summary>
    /// <param name="command">Forgot password request containing email</param>
    /// <returns>Success message</returns>
    /// <response code="200">Password reset email sent successfully</response>
    /// <response code="400">Invalid email format</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        SetNoStore();
        await _mediator.Send(command);
        return Ok(new ForgotPasswordSuccessResponse(
            "If an account matches, password reset instructions will be sent.", true));
    }

    /// <summary>
    /// Reset user password using reset token
    /// </summary>
    /// <param name="command">Reset password request containing token and new password</param>
    /// <returns>Success message</returns>
    /// <response code="200">Password reset successfully</response>
    /// <response code="400">Invalid token or password requirements not met</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
    {
        SetNoStore();
        if (!IsResetIdempotencyKeyValid(idempotencyKey)) return InvalidResetToken();
        try
        {
            await _mediator.Send(command);
            return Ok(new ResetPasswordSuccessResponse("Password reset completed.", true));
        }
        catch (ValidationException)
        {
            return InvalidResetToken();
        }
    }

    /// <summary>
    /// Validate reset token without resetting password
    /// </summary>
    /// <param name="request">Reset token validation request</param>
    /// <returns>Token validation result</returns>
    /// <response code="200">Token is valid</response>
    /// <response code="400">Token is invalid or expired</response>
    [HttpPost("validate-reset-token")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> ValidateResetToken([FromBody] ValidateResetTokenRequest request)
    {
        SetNoStore();
        var token = await _mediator.Send(new ValidateResetTokenQuery(request.Token));
        return token ? Ok(new ResetTokenValidityResponse(true)) : InvalidResetToken();
    }

    private void SetNoStore()
    {
        Response.Headers.CacheControl = "no-store, max-age=0";
        Response.Headers.Pragma = "no-cache";
    }

    private ObjectResult InvalidResetToken() => new(new
    {
        type = "https://bookspot.example/problems/reset-token-invalid",
        title = "Reset token invalid",
        status = 400,
        detail = "The reset capability is invalid.",
        code = "reset_token_invalid"
    })
    {
        StatusCode = StatusCodes.Status400BadRequest,
        ContentTypes = { "application/problem+json" }
    };

    private static bool IsResetIdempotencyKeyValid(string value) =>
        value is { Length: 32 } && value.All(character =>
            character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_');
}
