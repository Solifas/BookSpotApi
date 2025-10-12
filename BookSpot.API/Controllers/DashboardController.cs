using System;
using System.Collections.Generic;

using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.DTOs.Dashboard;
using BookSpot.Application.Features.Dashboard.Queries;
using BookSpot.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookSpot.API.Controllers;

/// <summary>
/// Dashboard controller for provider statistics and analytics
/// </summary>
[ApiController]
[Route("dashboard")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IClaimsService _claimsService;

    public DashboardController(IMediator mediator, IClaimsService claimsService)
    {
        _mediator = mediator;
        _claimsService = claimsService;
    }

    /// <summary>
    /// Get dashboard statistics for a specific provider
    /// </summary>
    /// <param name="providerId">Provider's user ID</param>
    /// <returns>Dashboard statistics including bookings, revenue, and client metrics</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    /// <response code="404">Provider not found</response>
    /// <response code="400">Invalid provider ID</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Only providers can access dashboard stats</response>
    [HttpGet("provider/{providerId}/stats")]
    [Authorize]
    [ProducesResponseType(typeof(DashboardStats), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<DashboardStats>> GetProviderStats(string providerId)
    {
        var query = new GetProviderDashboardStatsQuery(providerId);
        var stats = await _mediator.Send(query);
        return Ok(stats);
    }

    /// <summary>
    /// Get provider insights with optional date filters
    /// </summary>
    /// <param name="providerId">Provider's user ID</param>
    /// <param name="startDate">Filter bookings from this date (inclusive)</param>
    /// <param name="endDate">Filter bookings up to this date (inclusive)</param>
    /// <returns>Aggregated statistics and top services for the provider</returns>
    /// <response code="200">Insights retrieved successfully</response>
    /// <response code="400">Invalid filter values</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Authenticated user does not match providerId</response>
    /// <response code="404">Provider not found</response>
    [HttpGet("providers/{providerId}/insights")]
    [Authorize]
    [ProducesResponseType(typeof(ProviderInsightsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderInsightsResponse>> GetProviderInsights(
        string providerId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        if (!_claimsService.IsAuthenticated())
        {
            return Unauthorized(new { error = "Unauthorized", message = "Authentication token is missing or invalid." });
        }

        var currentUserId = _claimsService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { error = "Unauthorized", message = "Authentication token is missing or invalid." });
        }

        if (!string.Equals(currentUserId, providerId, StringComparison.Ordinal))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden", message = "You are not allowed to access insights for this provider." });
        }

        var query = new GetProviderInsightsQuery(providerId, startDate, endDate);
        var insights = await _mediator.Send(query);
        return Ok(insights);
    }

    /// <summary>
    /// Get dashboard client records for the authenticated provider.
    /// </summary>
    /// <returns>List of dashboard clients.</returns>
    /// <response code="200">Clients retrieved successfully</response>
    /// <response code="400">Provider is not recognized as a service provider</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Only providers can access client records</response>
    /// <response code="404">Provider profile not found</response>
    /// <response code="500">Unexpected server error</response>
    [HttpGet("clients")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<DashboardClientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DashboardClientDto>>> GetClients()
    {
        if (!_claimsService.IsAuthenticated())
        {
            return Unauthorized(new { error = "Unauthorized", message = "Authentication token is missing or invalid." });
        }

        var providerId = _claimsService.GetCurrentUserId();
        if (string.IsNullOrEmpty(providerId))
        {
            return Unauthorized(new { error = "Unauthorized", message = "Authentication token is missing or invalid." });
        }

        if (!_claimsService.IsProvider())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden", message = "Only providers can access dashboard clients." });
        }

        try
        {
            var clients = await _mediator.Send(new GetDashboardClientsQuery(providerId));
            return Ok(clients);
        }
        catch (NotFoundException)
        {
            return NotFound(new { error = "NotFound", message = "Provider profile could not be found." });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = "ValidationError", message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "InternalServerError",
                message = "An unexpected error occurred while retrieving dashboard clients."
            });
        }
    }
}
