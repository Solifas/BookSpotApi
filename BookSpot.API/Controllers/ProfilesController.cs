using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Features.Profiles.Commands;
using BookSpot.Application.Features.Profiles.Queries;
using BookSpot.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

[ApiController]
[Route("profiles")]
public class ProfilesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IClaimsService _claimsService;

    public ProfilesController(IMediator mediator, IClaimsService claimsService)
    {
        _mediator = mediator;
        _claimsService = claimsService;
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<Profile>> Get(string id)
    {
        // Users can only access their own profile or providers can access any profile
        var currentUserId = _claimsService.GetCurrentUserId();
        if (currentUserId != id && !_claimsService.IsProvider())
        {
            return Forbid();
        }

        var profile = await _mediator.Send(new GetProfileQuery(id));
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<Profile>> GetCurrentUser()
    {
        var currentUserId = _claimsService.GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized();
        }

        var profile = await _mediator.Send(new GetProfileQuery(currentUserId));
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPost]
    public async Task<ActionResult<Profile>> Post([FromBody] CreateProfileCommand command)
    {
        var profile = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id = profile.Id }, profile);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Profile>> Put(string id, [FromBody] UpdateProfileCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch");
        var profile = await _mediator.Send(command);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _mediator.Send(new DeleteProfileCommand(id));
        return deleted ? NoContent() : NotFound();
    }
}

