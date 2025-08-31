using BookSpot.Application.Features.Profiles.Commands;
using BookSpot.Application.Features.Profiles.Queries;
using BookSpot.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.Function.Controllers;

[ApiController]
[Route("profiles")]
public class ProfilesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Profile>> Get(string id)
    {
        var profile = await _mediator.Send(new GetProfileQuery(id));
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

