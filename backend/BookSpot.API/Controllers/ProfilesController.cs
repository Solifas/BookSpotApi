using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.DTOs.Auth;
using BookSpot.Application.DTOs.Profiles;
using BookSpot.Application.Features.Profiles.Commands;
using BookSpot.Application.Features.Profiles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

[ApiController]
[Route("profiles")]
[Authorize(Policy = "ClientOrProvider")]
public class ProfilesController(IMediator mediator, IClaimsService claimsService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ProfileDto>> GetCurrentUser()
    {
        var id = claimsService.GetCurrentUserId();
        if (id is null) return Unauthorized();
        var profile = await mediator.Send(new GetProfileQuery(id));
        return profile is null ? NotFound() : Ok(ProfileDto.From(profile));
    }

    [HttpPatch("me")]
    public async Task<ActionResult<ProfileDto>> PatchCurrentUser([FromBody] UpdateMyProfileRequest request)
    {
        var id = claimsService.GetCurrentUserId();
        if (id is null) return Unauthorized();
        var profile = await mediator.Send(new UpdateProfileCommand(id, request.FullName, request.ContactNumber));
        return profile is null ? NotFound() : Ok(ProfileDto.From(profile));
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteCurrentUser()
    {
        var id = claimsService.GetCurrentUserId();
        if (id is null) return Unauthorized();
        return await mediator.Send(new DeleteProfileCommand(id)) ? NoContent() : NotFound();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProfileDto>> Get(string id)
    {
        if (!IsSelf(id)) return NotFound();
        var profile = await mediator.Send(new GetProfileQuery(id));
        return profile is null ? NotFound() : Ok(ProfileDto.From(profile));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProfileDto>> Put(string id, [FromBody] UpdateMyProfileRequest request)
    {
        if (!IsSelf(id)) return NotFound();
        var profile = await mediator.Send(new UpdateProfileCommand(id, request.FullName, request.ContactNumber));
        return profile is null ? NotFound() : Ok(ProfileDto.From(profile));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!IsSelf(id)) return NotFound();
        return await mediator.Send(new DeleteProfileCommand(id)) ? NoContent() : NotFound();
    }

    private bool IsSelf(string id) =>
        string.Equals(claimsService.GetCurrentUserId(), id, StringComparison.Ordinal);
}
