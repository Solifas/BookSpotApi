using BookSpot.Application.Features.Businesses.Commands;
using BookSpot.Application.Features.Businesses.Queries;
using BookSpot.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.Function.Controllers;

[ApiController]
[Route("businesses")]
public class BusinessesController : ControllerBase
{
    private readonly IMediator _mediator;
    public BusinessesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id}")]
    public async Task<ActionResult<Business>> Get(string id)
    {
        var business = await _mediator.Send(new GetBusinessQuery(id));
        return business is null ? NotFound() : Ok(business);
    }

    [HttpPost]
    public async Task<ActionResult<Business>> Post([FromBody] CreateBusinessCommand command)
    {
        var business = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id = business.Id }, business);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Business>> Put(string id, [FromBody] UpdateBusinessCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch");
        var updated = await _mediator.Send(command);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _mediator.Send(new DeleteBusinessCommand(id));
        return deleted ? NoContent() : NotFound();
    }
}
