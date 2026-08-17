using BookSpot.Application.Features.Businesses.Commands;
using BookSpot.Application.Features.Businesses.Queries;
using BookSpot.Application.Features.Services.Queries;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.DTOs.Canonical;
using BookSpot.Application.Features.Canonical.Queries;
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
    private readonly IClaimsService _claims;
    public BusinessesController(IMediator mediator, IClaimsService claims)
    {
        _mediator = mediator;
        _claims = claims;
    }

    [HttpGet("mine")]
    [Authorize(Policy = "ProviderOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<BusinessDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BusinessDto>>> GetMine() =>
        Ok(await _mediator.Send(new GetMyBusinessesQuery(_claims.GetCurrentUserId()!)));

    /// <summary>
    /// Get business by ID
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <returns>Business details</returns>
    /// <response code="200">Business found</response>
    /// <response code="404">Business not found</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BusinessDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<BusinessDto>> Get(string id)
    {
        var business = await _mediator.Send(new GetBusinessQuery(id));
        if (business is null || !business.IsActive)
            throw new BookSpot.Application.Exceptions.NotFoundException("Business", id);
        return Ok(CanonicalDtoMapper.ToBusinessDto(business));
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
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetServices(string id)
    {
        var business = await _mediator.Send(new GetBusinessQuery(id));
        if (business is null || !business.IsActive)
            throw new BookSpot.Application.Exceptions.NotFoundException("Business", id);
        var services = await _mediator.Send(new GetServicesByBusinessQuery(id));
        var provider = await _mediator.Send(new BookSpot.Application.Features.Profiles.Queries.GetProfileQuery(business.ProviderId));
        return Ok(services.Where(service => service.IsActive)
            .Select(service => CanonicalDtoMapper.ToServiceDto(service, business,
                provider?.FullName ?? service.ProviderName)).ToArray());
    }

    /// <summary>
    /// Get all services offered by a specific provider across all their businesses
    /// </summary>
    /// <param name="providerId">Provider's user ID</param>
    /// <returns>List of services offered by the provider</returns>
    /// <response code="200">Services retrieved successfully</response>
    /// <response code="404">Provider not found</response>
    [HttpGet("provider/{providerId}/services")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetServicesByProvider(string providerId)
    {
        var services = await _mediator.Send(new GetServicesByProviderQuery(providerId));
        var result = new List<ServiceDto>();
        foreach (var service in services.Where(value => value.IsActive))
        {
            var business = await _mediator.Send(new GetBusinessQuery(service.BusinessId));
            if (business is null || !business.IsActive || business.ProviderId != providerId) continue;
            var provider = await _mediator.Send(new BookSpot.Application.Features.Profiles.Queries.GetProfileQuery(providerId));
            result.Add(CanonicalDtoMapper.ToServiceDto(service, business, provider?.FullName ?? service.ProviderName));
        }
        Response.Headers["Deprecation"] = "true";
        Response.Headers["Sunset"] = "Wed, 31 Mar 2027 00:00:00 GMT";
        return Ok(result);
    }

    /// <summary>
    /// Create a new business
    /// </summary>
    /// <param name="request">Business creation details</param>
    /// <returns>Created business</returns>
    /// <response code="201">Business created successfully</response>
    /// <response code="400">Invalid input or validation errors</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Only providers can create businesses</response>
    [HttpPost]
    [Authorize(Policy = "ProviderOnly")]
    [ProducesResponseType(typeof(BusinessDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<BusinessDto>> Post([FromBody] CreateBusinessRequest request)
    {
        var business = await _mediator.Send(new CreateBusinessCommand(request.BusinessName, request.Description,
            request.Address, request.Phone, request.Email, request.City, request.Website, request.ImageUrl,
            request.IsActive));
        return CreatedAtAction(nameof(Get), new { id = business.Id }, CanonicalDtoMapper.ToBusinessDto(business));
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
    [Authorize(Policy = "ProviderOnly")]
    [ProducesResponseType(typeof(BusinessDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<BusinessDto>> Put(string id, [FromBody] UpdateBusinessCommand command)
    {
        if (id != command.Id) throw new BookSpot.Application.Exceptions.ValidationException("Id mismatch.");
        var updated = await _mediator.Send(command);
        if (updated is null) throw new BookSpot.Application.Exceptions.NotFoundException("Business", id);
        Response.Headers["Deprecation"] = "true";
        Response.Headers["Sunset"] = "Wed, 31 Mar 2027 00:00:00 GMT";
        return Ok(CanonicalDtoMapper.ToBusinessDto(updated));
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = "ProviderOnly")]
    [ProducesResponseType(typeof(BusinessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessDto>> Patch(string id, [FromBody] UpdateBusinessRequest request)
    {
        if (request.BusinessName is null && request.Description is null && request.Address is null &&
            request.Phone is null && request.Email is null && request.City is null && request.Website is null &&
            request.ImageUrl is null && request.IsActive is null)
            throw new BookSpot.Application.Exceptions.ValidationException(
                new Dictionary<string, string[]> { ["$"] = ["empty_patch"] });
        var updated = await _mediator.Send(new UpdateBusinessCommand(id, request.BusinessName, request.Description,
            request.Address, request.Phone, request.Email, request.City, request.Website, request.ImageUrl, request.IsActive));
        if (updated is null) throw new BookSpot.Application.Exceptions.NotFoundException("Business", id);
        return Ok(CanonicalDtoMapper.ToBusinessDto(updated));
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
    [Authorize(Policy = "ProviderOnly")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!await _mediator.Send(new DeleteBusinessCommand(id)))
            throw new BookSpot.Application.Exceptions.NotFoundException("Business", id);
        return NoContent();
    }
}
