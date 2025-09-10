using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using MediatR;

namespace BookSpot.Application.Features.Services.Commands;

public record CreateServiceCommand(string BusinessId, string Name, decimal Price, int DurationMinutes) : IRequest<Service>;

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

        // Validate that the business exists
        Business business = await _businesses.GetAsync(request.BusinessId) ?? throw new NotFoundException($"Business with ID '{request.BusinessId}' not found.");

        // Validate that the current user owns the business
        if (business.ProviderId != currentUserId)
        {
            throw new ValidationException("You can only create services for your own businesses.");
        }

        // Validate that the business is active
        if (!business.IsActive)
        {
            throw new ValidationException($"Business with ID '{request.BusinessId}' is not active and cannot offer new services.");
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
            BusinessId = request.BusinessId,
            Name = request.Name,
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            CreatedAt = DateTime.UtcNow
        };

        await _services.SaveAsync(service);
        return service;
    }
}