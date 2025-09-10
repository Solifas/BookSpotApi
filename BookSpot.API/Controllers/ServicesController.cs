using BookSpot.Application.Features.Services.Commands;
using BookSpot.Application.Features.Services.Queries;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

[ApiController]
[Route("services")]
public class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Service>>> GetAll()
    {
        var services = await _mediator.Send(new GetAllServicesQuery());
        return Ok(services);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Service>> Get(string id)
    {
        var service = await _mediator.Send(new GetServiceQuery(id));
        if (service is null)
            throw new NotFoundException("Service", id);

        return Ok(service);
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
        if (id != command.Id)
            throw new ValidationException("The ID in the URL does not match the ID in the request body.");

        var updated = await _mediator.Send(command);
        if (updated is null)
            throw new NotFoundException("Service", id);

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _mediator.Send(new DeleteServiceCommand(id));
        if (!deleted)
            throw new NotFoundException("Service", id);

        return NoContent();
    }
}
