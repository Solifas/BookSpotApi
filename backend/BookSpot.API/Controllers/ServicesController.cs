using BookSpot.Application.DTOs.Canonical;
using BookSpot.Application.Exceptions;
using BookSpot.Application.Features.Businesses.Queries;
using BookSpot.Application.Features.Canonical.Queries;
using BookSpot.Application.Features.Services.Commands;
using BookSpot.Application.Features.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

[ApiController]
[Route("services")]
[Produces("application/json")]
public class ServicesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceSearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceSearchResponse>> GetAll() =>
        Ok(await mediator.Send(new SearchServicesQuery(Page: 1, PageSize: 100)));

    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceSearchResponse>> Search(
        [FromQuery(Name = "q")] string? q = null, [FromQuery] string? name = null,
        [FromQuery] string? category = null,
        [FromQuery] string? city = null, [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null, [FromQuery] int? minDuration = null,
        [FromQuery] int? maxDuration = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page is < 1 or > 100000 || pageSize is < 1 or > 100)
            throw new ValidationException("Invalid pagination values.");
        if (q is not null && name is not null)
            throw new ValidationException(new Dictionary<string, string[]> { ["q"] = ["mutually_exclusive"] });
        var searchText = q ?? name;
        if (searchText is not null && (string.IsNullOrWhiteSpace(searchText) || searchText.Length > 200))
            throw new ValidationException(new Dictionary<string, string[]> { [q is null ? "name" : "q"] = ["invalid"] });
        if (category is not null && (string.IsNullOrWhiteSpace(category) || category.Length > 100) ||
            city is not null && (string.IsNullOrWhiteSpace(city) || city.Length > 100))
            throw new ValidationException("Invalid search text filter.");
        if (minPrice is < 0 or > 1_000_000 || maxPrice is < 0 or > 1_000_000 || minPrice > maxPrice)
            throw new ValidationException("Invalid price range.");
        if (!ValidDuration(minDuration) || !ValidDuration(maxDuration) || minDuration > maxDuration)
            throw new ValidationException("Invalid duration range.");
        if (name is not null)
        {
            Response.Headers["Deprecation"] = "true";
            Response.Headers["Sunset"] = "Wed, 31 Mar 2027 00:00:00 GMT";
        }
        return Ok(await mediator.Send(new SearchServicesQuery(searchText?.Trim(), category?.Trim(), city?.Trim(), minPrice, maxPrice,
            minDuration, maxDuration, page, pageSize)));
    }

    private static bool ValidDuration(int? value) => value is null || value is >= 15 and <= 480 && value % 15 == 0;

    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> Get(string id)
    {
        var service = await mediator.Send(new GetServiceQuery(id));
        if (service is null || !service.IsActive) throw new NotFoundException("Service", id);
        return Ok(await ToDto(service));
    }

    [HttpPost]
    [Authorize(Policy = "ProviderOnly")]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ServiceDto>> Post([FromBody] CreateServiceRequest request)
    {
        var service = await mediator.Send(new CreateServiceCommand(request.BusinessId, request.Name,
            request.Description, request.Category, request.PriceAmount, request.DurationMinutes, request.ImageUrl,
            request.Tags, request.Location, request.IsActive));
        return CreatedAtAction(nameof(Get), new { id = service.Id }, await ToDto(service));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "ProviderOnly")]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceDto>> Put(string id, [FromBody] UpdateServiceCommand command)
    {
        if (id != command.Id) throw new ValidationException("The ID in the URL does not match the ID in the request body.");
        var updated = await mediator.Send(command);
        if (updated is null) throw new NotFoundException("Service", id);
        Response.Headers["Deprecation"] = "true";
        Response.Headers["Sunset"] = "Wed, 31 Mar 2027 00:00:00 GMT";
        return Ok(await ToDto(updated));
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = "ProviderOnly")]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceDto>> Patch(string id, [FromBody] UpdateServiceRequest request)
    {
        if (request.Name is null && request.Description is null && request.Category is null &&
            request.PriceAmount is null && request.DurationMinutes is null && request.ImageUrl is null &&
            request.Tags is null && request.Location is null && request.IsActive is null)
            throw new ValidationException(new Dictionary<string, string[]> { ["$"] = ["empty_patch"] });
        var updated = await mediator.Send(new UpdateServiceCommand(id, request.Name, request.Description,
            request.Category, request.PriceAmount, request.DurationMinutes, request.ImageUrl, request.Tags,
            request.Location, request.IsActive));
        if (updated is null) throw new NotFoundException("Service", id);
        return Ok(await ToDto(updated));
    }

    [HttpGet("{id}/availability")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceAvailabilityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceAvailabilityDto>> GetAvailability(string id, [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to)
    {
        if (!HasExplicitWholeSecondOffset("from") || !HasExplicitWholeSecondOffset("to"))
            throw new ValidationException(new Dictionary<string, string[]> { ["from"] = ["invalid_timestamp"] });
        return Ok(await mediator.Send(new GetServiceAvailabilityQuery(id, from, to)));
    }

    private bool HasExplicitWholeSecondOffset(string name) =>
        System.Text.RegularExpressions.Regex.IsMatch(Request.Query[name].ToString(),
            @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:Z|[+-]\d{2}:\d{2})$");

    [HttpDelete("{id}")]
    [Authorize(Policy = "ProviderOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!await mediator.Send(new DeleteServiceCommand(id))) throw new NotFoundException("Service", id);
        return NoContent();
    }

    private async Task<ServiceDto> ToDto(BookSpot.Domain.Entities.Service service)
    {
        var business = await mediator.Send(new GetBusinessQuery(service.BusinessId));
        if (business is null || !business.IsActive) throw new NotFoundException("Service", service.Id);
        var provider = await mediator.Send(new BookSpot.Application.Features.Profiles.Queries.GetProfileQuery(business.ProviderId));
        return CanonicalDtoMapper.ToServiceDto(service, business, provider?.FullName ?? service.ProviderName);
    }
}
