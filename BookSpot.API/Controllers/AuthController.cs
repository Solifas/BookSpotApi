using BookSpot.Application.DTOs.Auth;
using BookSpot.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

/// <summary>
/// Authentication controller for user registration and login
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
    /// Register a new user account
    /// </summary>
    /// <param name="request">Registration details including email, full name, optional contact number, password, and user type</param>
    /// <returns>Authentication response with JWT token and user information</returns>
    /// <response code="200">User successfully registered and authenticated</response>
    /// <response code="400">Invalid input or validation errors</response>
    /// <response code="409">User with email already exists</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(request.Email, request.FullName, request.ContactNumber, request.Password, request.UserType);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Authenticate user and get JWT token
    /// </summary>
    /// <param name="request">Login credentials (email and password)</param>
    /// <returns>Authentication response with JWT token and user information</returns>
    /// <response code="200">User successfully authenticated</response>
    /// <response code="400">Invalid credentials or validation errors</response>
    /// <response code="401">Authentication failed</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var response = await _mediator.Send(command);
        return Ok(response);
    }
}