using BookSpot.Application.Features.Reviews.Commands;
using BookSpot.Application.Features.Reviews.Queries;

using BookSpot.Application.DTOs.Canonical;
using BookSpot.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

[ApiController]
[Route("reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReviewsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReviewDto>> Get(string id)
    {
        var review = await _mediator.Send(new GetReviewQuery(id));
        if (review is null) throw new NotFoundException("Review", id);
        return Ok(CanonicalDtoMapper.ToReviewDto(review));
    }

    [HttpPost]
    [Authorize(Policy = "ClientOnly")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ReviewDto>> Post([FromBody] CreateReviewCommand command)
    {
        var review = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id = review.Id }, CanonicalDtoMapper.ToReviewDto(review));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "ClientOnly")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReviewDto>> Put(string id, [FromBody] UpdateReviewCommand command)
    {
        if (id != command.Id) throw new ValidationException("Id mismatch.");
        var updated = await _mediator.Send(command);
        if (updated is null) throw new NotFoundException("Review", id);
        Response.Headers["Deprecation"] = "true";
        Response.Headers["Sunset"] = "Wed, 31 Mar 2027 00:00:00 GMT";
        return Ok(CanonicalDtoMapper.ToReviewDto(updated));
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = "ClientOnly")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReviewDto>> Patch(string id, [FromBody] UpdateReviewRequest request)
    {
        if (request.Rating is null && request.Comment is null)
            throw new ValidationException("At least one review field is required.");
        if (request.Rating is < 1 or > 5)
            throw new ValidationException(new Dictionary<string, string[]> { ["rating"] = ["out_of_range"] });
        if (request.Comment is not null && string.IsNullOrWhiteSpace(request.Comment))
            throw new ValidationException(new Dictionary<string, string[]> { ["comment"] = ["blank"] });
        if (request.Comment?.Length > 2000)
            throw new ValidationException(new Dictionary<string, string[]> { ["comment"] = ["too_long"] });
        var existing = await _mediator.Send(new GetReviewQuery(id));
        if (existing is null) throw new NotFoundException("Review", id);
        var updated = await _mediator.Send(new UpdateReviewCommand(id, request.Rating ?? existing.Rating,
            request.Comment?.Trim() ?? existing.Comment));
        if (updated is null) throw new NotFoundException("Review", id);
        return Ok(CanonicalDtoMapper.ToReviewDto(updated));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!await _mediator.Send(new DeleteReviewCommand(id))) throw new NotFoundException("Review", id);
        return NoContent();
    }
}
