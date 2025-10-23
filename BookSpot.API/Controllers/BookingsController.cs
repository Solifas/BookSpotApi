using BookSpot.Application.DTOs.Bookings;
using BookSpot.Application.Features.Bookings.Commands;
using BookSpot.Application.Features.Bookings.Queries;
using BookSpot.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

/// <summary>
/// Bookings controller for managing appointment bookings
/// </summary>
[ApiController]
[Route("bookings")]
[Produces("application/json")]
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;
    public BookingsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get booking by ID
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Booking details</returns>
    /// <response code="200">Booking found</response>
    /// <response code="404">Booking not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Booking), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<Booking>> Get(string id)
    {
        var booking = await _mediator.Send(new GetBookingQuery(id));
        return booking is null ? NotFound() : Ok(booking);
    }

    /// <summary>
    /// Get all bookings for a specific provider with optional filters
    /// </summary>
    /// <param name="providerId">Provider's user ID</param>
    /// <param name="status">Filter by booking status (pending, confirmed, completed, cancelled)</param>
    /// <param name="startDate">Filter from date (ISO format)</param>
    /// <param name="endDate">Filter until date (ISO format)</param>
    /// <returns>Array of BookingWithDetails with full service, client, and business info</returns>
    /// <response code="200">Bookings retrieved successfully</response>
    /// <response code="404">Provider not found</response>
    /// <response code="400">Invalid provider or parameters</response>
    [HttpGet("provider/{providerId}")]
    [Authorize(Policy = "ProviderOnly")]
    [ProducesResponseType(typeof(IEnumerable<BookingWithDetails>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<IEnumerable<BookingWithDetails>>> GetProviderBookings(
        string providerId,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = new GetProviderBookingsQuery(providerId, status, startDate, endDate);
        var bookings = await _mediator.Send(query);
        return Ok(bookings);
    }

    /// <summary>
    /// Get all bookings for a specific client (authenticated client can only view their own bookings)
    /// </summary>
    /// <param name="clientId">Client's user ID</param>
    /// <param name="status">Filter by booking status (pending, confirmed, completed, cancelled)</param>
    /// <param name="startDate">Filter from date (ISO format)</param>
    /// <param name="endDate">Filter until date (ISO format)</param>
    /// <returns>Array of BookingWithDetails with full service, provider, and business info</returns>
    /// <response code="200">Bookings retrieved successfully</response>
    /// <response code="400">Invalid client ID or access denied</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Can only view own bookings</response>
    /// <response code="404">Client not found</response>
    [HttpGet("client/{clientId}")]
    [Authorize(Policy = "ClientOnly")]
    [ProducesResponseType(typeof(IEnumerable<BookingWithDetails>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<BookingWithDetails>>> GetClientBookings(
        string clientId,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = new GetClientBookingsQuery(clientId, status, startDate, endDate);
        var bookings = await _mediator.Send(query);
        return Ok(bookings);
    }

    /// <summary>
    /// Create a new booking
    /// </summary>
    /// <param name="command">Booking creation details</param>
    /// <returns>Created booking</returns>
    /// <response code="201">Booking created successfully</response>
    /// <response code="400">Invalid input or validation errors</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Only clients can create bookings</response>
    [HttpPost]
    [Authorize(Policy = "ClientOrProvider")]
    [ProducesResponseType(typeof(Booking), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<Booking>> Post([FromBody] CreateBookingCommand command)
    {
        var booking = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id = booking.Id }, booking);
    }

    /// <summary>
    /// Update an existing booking
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="command">Booking update details</param>
    /// <returns>Updated booking</returns>
    /// <response code="200">Booking updated successfully</response>
    /// <response code="400">Invalid input or ID mismatch</response>
    /// <response code="404">Booking not found</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [HttpPut("{id}")]
    [Authorize(Policy = "ClientOrProvider")]
    [ProducesResponseType(typeof(Booking), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<Booking>> Put(string id, [FromBody] UpdateBookingCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch");
        var updated = await _mediator.Send(command);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>
    /// Delete a booking
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Booking deleted successfully</response>
    /// <response code="404">Booking not found</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [HttpDelete("{id}")]
    [Authorize(Policy = "ClientOrProvider")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _mediator.Send(new DeleteBookingCommand(id));
        return deleted ? NoContent() : NotFound();
    }
}
