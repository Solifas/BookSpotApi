using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;
using MediatR;

namespace BookSpot.Application.Features.Services.Commands;

public record UpdateServiceCommand(
    string Id,
    string Name,
    string Description,
    string? Category,
    decimal Price,
    int DurationMinutes,
    string? ImageUrl = null,
    List<string>? Tags = null,
    bool IsActive = true
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

        // Update service properties
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Category = request.Category;
        existing.Price = request.Price;
        existing.DurationMinutes = request.DurationMinutes;
        existing.ImageUrl = request.ImageUrl;
        existing.Tags = request.Tags ?? new List<string>();
        existing.IsActive = request.IsActive;

        await _services.SaveAsync(existing);
        return existing;
    }
}
