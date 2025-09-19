using BookSpot.Application.Features.Services.Commands;
using BookSpot.Application.Features.Services.Queries;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

/// <summary>
/// Services controller for managing service offerings
/// </summary>
[ApiController]
[Route("services")]
[Produces("application/json")]
public class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServicesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get all available services
    /// </summary>
    /// <returns>List of all services</returns>
    /// <response code="200">Services retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Service>), 200)]
    public async Task<ActionResult<IEnumerable<Service>>> GetAll()
    {
        var services = await _mediator.Send(new GetAllServicesQuery());
        return Ok(services);
    }

    /// <summary>
    /// Search services with filters
    /// </summary>
    /// <param name="name">Filter by service name (partial match)</param>
    /// <param name="city">Filter by business city (partial match)</param>
    /// <param name="minPrice">Minimum price filter</param>
    /// <param name="maxPrice">Maximum price filter</param>
    /// <param name="minDuration">Minimum duration in minutes</param>
    /// <param name="maxDuration">Maximum duration in minutes</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <returns>Filtered list of services</returns>
    /// <response code="200">Services retrieved successfully</response>
    /// <response code="400">Invalid search parameters</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<Service>), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<IEnumerable<Service>>> Search(
        [FromQuery] string? name = null,
        [FromQuery] string? city = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int? minDuration = null,
        [FromQuery] int? maxDuration = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageSize > 100) pageSize = 100;
        if (page < 1) page = 1;

        var query = new SearchServicesQuery(name, city, minPrice, maxPrice, minDuration, maxDuration, page, pageSize);
        var services = await _mediator.Send(query);
        return Ok(services);
    }

    /// <summary>
    /// Get service by ID
    /// </summary>
    /// <param name="id">Service ID</param>
    /// <returns>Service details</returns>
    /// <response code="200">Service found</response>
    /// <response code="404">Service not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Service), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<Service>> Get(string id)
    {
        var service = await _mediator.Send(new GetServiceQuery(id));
        if (service is null)
            throw new NotFoundException("Service", id);

        return Ok(service);
    }

    /// <summary>
    /// Create a new service
    /// </summary>
    /// <param name="command">Service creation details</param>
    /// <returns>Created service</returns>
    /// <response code="201">Service created successfully</response>
    /// <response code="400">Invalid input or validation errors</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Only business owners can create services</response>
    [HttpPost]
    [ProducesResponseType(typeof(Service), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<Service>> Post([FromBody] CreateServiceCommand command)
    {
        var service = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id = service.Id }, service);
    }

    /// <summary>
    /// Update an existing service
    /// </summary>
    /// <param name="id">Service ID</param>
    /// <param name="command">Service update details</param>
    /// <returns>Updated service</returns>
    /// <response code="200">Service updated successfully</response>
    /// <response code="400">Invalid input or ID mismatch</response>
    /// <response code="404">Service not found</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Only service owner can update</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Service), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<Service>> Put(string id, [FromBody] UpdateServiceCommand command)
    {
        if (id != command.Id)
            throw new ValidationException("The ID in the URL does not match the ID in the request body.");

        var updated = await _mediator.Send(command);
        if (updated is null)
            throw new NotFoundException("Service", id);

        return Ok(updated);
    }

    /// <summary>
    /// Delete a service
    /// </summary>
    /// <param name="id">Service ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Service deleted successfully</response>
    /// <response code="404">Service not found</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Only service owner can delete</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _mediator.Send(new DeleteServiceCommand(id));
        if (!deleted)
            throw new NotFoundException("Service", id);

        return NoContent();
    }
}
