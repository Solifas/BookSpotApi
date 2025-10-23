using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;
using MediatR;

namespace BookSpot.Application.Features.Services.Commands;

public record CreateServiceCommand(
    string BusinessId,
    string Name,
    string Description,
    string? Category,
    decimal Price,
    int DurationMinutes,
    string? ImageUrl = null,
    List<string>? Tags = null,
    bool IsActive = true
) : IRequest<Service>;

public class CreateServiceHandler : IRequestHandler<CreateServiceCommand, Service>
{
    private readonly IServiceRepository _services;
    private readonly IBusinessRepository _businesses;
    private readonly IClaimsService _claimsService;

    public CreateServiceHandler(IServiceRepository services, IBusinessRepository businesses, IClaimsService claimsService)
    {
        _services = services;
        _businesses = businesses;
        _claimsService = claimsService;
    }

    public async Task<Service> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        // Get current user from JWT claims
        string? currentUserId = _claimsService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new ValidationException("User must be authenticated to create a service.");
        }

        // Validate that the current user is a provider
        if (!_claimsService.IsProvider())
        {
            throw new ValidationException("Only providers can create services.");
        }

        // Validate that the current user owns the business
        if (request.BusinessId != currentUserId)
        {
            throw new ValidationException("You can only create services for your own businesses.");
        }

        // Validate service data
        if (request.Price < 0)
        {
            throw new ValidationException("Service price cannot be negative.");
        }

        if (request.DurationMinutes <= 0)
        {
            throw new ValidationException("Service duration must be greater than 0 minutes.");
        }

        var service = new Service
        {
            Id = Guid.NewGuid().ToString(),
            // ProviderId = currentUserId,
            BusinessId = request.BusinessId,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            ImageUrl = request.ImageUrl,
            Tags = request.Tags ?? new List<string>(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _services.SaveAsync(service);
        return service;
    }
}