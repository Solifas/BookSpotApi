using BookSpot.Application.Features.Reviews.Commands;
using BookSpot.Application.Features.Reviews.Queries;
using BookSpot.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

[ApiController]
[Route("reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReviewsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id}")]
    public async Task<ActionResult<Review>> Get(string id)
    {
        var review = await _mediator.Send(new GetReviewQuery(id));
        return review is null ? NotFound() : Ok(review);
    }

    [HttpPost]
    public async Task<ActionResult<Review>> Post([FromBody] CreateReviewCommand command)
    {
        var review = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id = review.Id }, review);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Review>> Put(string id, [FromBody] UpdateReviewCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch");
        var updated = await _mediator.Send(command);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _mediator.Send(new DeleteReviewCommand(id));
        return deleted ? NoContent() : NotFound();
    }
}
