using BookSpot.Application.DTOs.Locations;
using BookSpot.Application.Features.Locations.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

/// <summary>
/// Locations controller for geographic and location-based data
/// </summary>
[ApiController]
[Route("locations")]
[Produces("application/json")]
public class LocationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LocationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all available cities with service information
    /// </summary>
    /// <returns>Array of cities with service counts and business information</returns>
    /// <response code="200">Cities retrieved successfully</response>
    [HttpGet("cities")]
    [ProducesResponseType(typeof(IEnumerable<CityInfo>), 200)]
    public async Task<ActionResult<IEnumerable<CityInfo>>> GetAvailableCities()
    {
        var query = new GetAvailableCitiesQuery();
        var cities = await _mediator.Send(query);
        return Ok(cities);
    }
}