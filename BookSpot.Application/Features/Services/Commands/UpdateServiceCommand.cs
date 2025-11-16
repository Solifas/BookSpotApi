using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;
using MediatR;

namespace BookSpot.Application.Features.Services.Commands;

public record UpdateServiceCommand(
    string Id,
    string? Name = null,
    string? Description = null,
    string? Category = null,
    decimal? Price = null,
    int? DurationMinutes = null,
    string? ImageUrl = null,
    List<string>? Tags = null,
    string? Location = null,
    bool? IsActive = null
) : IRequest<Service?>;

public class UpdateServiceHandler : IRequestHandler<UpdateServiceCommand, Service?>
{
    private readonly IServiceRepository _services;
    private readonly IBusinessRepository _businesses;
    private readonly IClaimsService _claimsService;

    public UpdateServiceHandler(IServiceRepository services, IBusinessRepository businesses, IClaimsService claimsService)
    {
        _services = services;
        _businesses = businesses;
        _claimsService = claimsService;
    }

    public async Task<Service?> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        // Get current user from JWT claims
        var currentUserId = _claimsService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new ValidationException("User must be authenticated to update a service.");
        }

        // Validate that the current user is a provider
        if (!_claimsService.IsProvider())
        {
            throw new ValidationException("Only providers can update services.");
        }

        var existing = await _services.GetAsync(request.Id);
        if (existing is null) return null;

        // Validate that the current user owns the service
        if (existing.ProviderId != currentUserId)
        {
            throw new ValidationException("You can only update your own services.");
        }

        // Update only the fields that are provided (partial update)
        if (request.Name != null)
            existing.Name = request.Name;

        if (request.Description != null)
            existing.Description = request.Description;

        if (request.Category != null)
            existing.Category = request.Category;

        if (request.Price.HasValue)
        {
            if (request.Price.Value < 0)
                throw new ValidationException("Service price cannot be negative.");
            existing.Price = request.Price.Value;
        }

        if (request.DurationMinutes.HasValue)
        {
            if (request.DurationMinutes.Value <= 0)
                throw new ValidationException("Service duration must be greater than 0 minutes.");
            existing.DurationMinutes = request.DurationMinutes.Value;
        }

        if (request.ImageUrl != null)
            existing.ImageUrl = request.ImageUrl;

        if (request.Tags != null)
            existing.Tags = request.Tags;

        if (request.Location != null)
            existing.Location = request.Location;

        if (request.IsActive.HasValue)
            existing.IsActive = request.IsActive.Value;

        await _services.SaveAsync(existing);
        return existing;
    }
}
