using BookSpot.Application.Features.Services.Commands;
using BookSpot.Application.Features.Services.Queries;
using BookSpot.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.Function.Controllers;

[ApiController]
[Route("services")]
public class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id}")]
    public async Task<ActionResult<Service>> Get(string id)
    {
        var service = await _mediator.Send(new GetServiceQuery(id));
        return service is null ? NotFound() : Ok(service);
    }

    [HttpPost]
    public async Task<ActionResult<Service>> Post([FromBody] CreateServiceCommand command)
    {
        var service = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id = service.Id }, service);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Service>> Put(string id, [FromBody] UpdateServiceCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch");
        var updated = await _mediator.Send(command);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _mediator.Send(new DeleteServiceCommand(id));
        return deleted ? NoContent() : NotFound();
    }
}
