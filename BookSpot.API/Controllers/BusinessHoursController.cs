using BookSpot.Application.Features.BusinessHours.Commands;
using BookSpot.Application.Features.BusinessHours.Queries;
using BookSpot.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.Function.Controllers;

[ApiController]
[Route("business-hours")]
public class BusinessHoursController : ControllerBase
{
    private readonly IMediator _mediator;
    public BusinessHoursController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id}")]
    public async Task<ActionResult<BusinessHour>> Get(string id)
    {
        var hour = await _mediator.Send(new GetBusinessHourQuery(id));
        return hour is null ? NotFound() : Ok(hour);
    }

    [HttpPost]
    public async Task<ActionResult<BusinessHour>> Post([FromBody] CreateBusinessHourCommand command)
    {
        var hour = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id = hour.Id }, hour);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BusinessHour>> Put(string id, [FromBody] UpdateBusinessHourCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch");
        var updated = await _mediator.Send(command);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _mediator.Send(new DeleteBusinessHourCommand(id));
        return deleted ? NoContent() : NotFound();
    }
}
