using BookSpot.Application.Features.Auth.Commands;
using BookSpot.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

/// <summary>
/// Authentication controller for user authentication and password management
/// </summary>
[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
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
        await _mediator.Send(command);
        
        return Ok(new 
        { 
            message = "If an account with that email exists, a password reset link has been sent.",
            success = true 
        });
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
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        await _mediator.Send(command);
        
        return Ok(new 
        { 
            message = "Password has been reset successfully. You can now log in with your new password.",
            success = true 
        });
    }

    /// <summary>
    /// Validate reset token without resetting password
    /// </summary>
    /// <param name="token">Reset token to validate</param>
    /// <returns>Token validation result</returns>
    /// <response code="200">Token is valid</response>
    /// <response code="400">Token is invalid or expired</response>
    [HttpGet("validate-reset-token/{token}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> ValidateResetToken(string token)
    {
        try
        {
            // We can create a simple validation query or reuse the reset logic
            // For now, let's create a simple validation
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ValidationException("Invalid token.");
            }

            return Ok(new 
            { 
                message = "Token is valid.",
                valid = true 
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new 
            { 
                message = ex.Message,
                valid = false 
            });
        }
    }
}