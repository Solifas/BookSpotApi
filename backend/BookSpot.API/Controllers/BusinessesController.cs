using BookSpot.Application.Features.Businesses.Commands;
using BookSpot.Application.Features.Businesses.Queries;
using BookSpot.Application.Features.Services.Queries;
using BookSpot.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

/// <summary>
/// Businesses controller for managing business entities
/// </summary>
[ApiController]
[Route("businesses")]
[Produces("application/json")]
public class BusinessesController : ControllerBase
{
    private readonly IMediator _mediator;
    public BusinessesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get business by ID
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <returns>Business details</returns>
    /// <response code="200">Business found</response>
    /// <response code="404">Business not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Business), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<Business>> Get(string id)
    {
        var business = await _mediator.Send(new GetBusinessQuery(id));
        return business is null ? NotFound() : Ok(business);
    }

    /// <summary>
    /// Get all services offered by a specific business
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <returns>List of services offered by the business</returns>
    /// <response code="200">Services retrieved successfully</response>
    /// <response code="404">Business not found</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Only business owner can access</response>
    [HttpGet("{id}/services")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<Service>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<IEnumerable<Service>>> GetServices(string id)
    {
        var services = await _mediator.Send(new GetServicesByBusinessQuery(id));
        return Ok(services);
    }

    /// <summary>
    /// Get all services offered by a specific provider across all their businesses
    /// </summary>
    /// <param name="providerId">Provider's user ID</param>
    /// <returns>List of services offered by the provider</returns>
    /// <response code="200">Services retrieved successfully</response>
    /// <response code="404">Provider not found</response>
    [HttpGet("provider/{providerId}/services")]
    [ProducesResponseType(typeof(IEnumerable<Service>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<Service>>> GetServicesByProvider(string providerId)
    {
        var services = await _mediator.Send(new GetServicesByProviderQuery(providerId));
        return Ok(services);
    }

    /// <summary>
    /// Create a new business
    /// </summary>
    /// <param name="command">Business creation details</param>
    /// <returns>Created business</returns>
    /// <response code="201">Business created successfully</response>
    /// <response code="400">Invalid input or validation errors</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Only providers can create businesses</response>
    [HttpPost]
    [ProducesResponseType(typeof(Business), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<Business>> Post([FromBody] CreateBusinessCommand command)
    {
        var business = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id = business.Id }, business);
    }

    /// <summary>
    /// Update an existing business
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <param name="command">Business update details</param>
    /// <returns>Updated business</returns>
    /// <response code="200">Business updated successfully</response>
    /// <response code="400">Invalid input or ID mismatch</response>
    /// <response code="404">Business not found</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Only business owner can update</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Business), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<Business>> Put(string id, [FromBody] UpdateBusinessCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch");
        var updated = await _mediator.Send(command);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>
    /// Delete a business
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Business deleted successfully</response>
    /// <response code="404">Business not found</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Only business owner can delete</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _mediator.Send(new DeleteBusinessCommand(id));
        return deleted ? NoContent() : NotFound();
    }
}
