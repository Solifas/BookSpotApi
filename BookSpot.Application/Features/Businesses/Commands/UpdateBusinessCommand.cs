using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using MediatR;

namespace BookSpot.Application.Features.Businesses.Commands;

public record UpdateBusinessCommand(
    string Id, 
    string BusinessName, 
    string Description, 
    string Address, 
    string Phone, 
    string Email, 
    string City, 
    string? Website = null,
    string? ImageUrl = null,
    bool IsActive = true
) : IRequest<Business?>;

public class UpdateBusinessHandler : IRequestHandler<UpdateBusinessCommand, Business?>
{
    private readonly IBusinessRepository _businesses;
    private readonly IClaimsService _claimsService;

    public UpdateBusinessHandler(IBusinessRepository businesses, IClaimsService claimsService)
    {
        _businesses = businesses;
        _claimsService = claimsService;
    }

    public async Task<Business?> Handle(UpdateBusinessCommand request, CancellationToken cancellationToken)
    {
        // Get current user from JWT claims
        var currentUserId = _claimsService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new ValidationException("User must be authenticated to update a business.");
        }

        // Validate that the current user is a provider
        if (!_claimsService.IsProvider())
        {
            throw new ValidationException("Only providers can update businesses.");
        }

        var existing = await _businesses.GetAsync(request.Id);
        if (existing is null) return null;

        // Validate that the current user owns the business
        if (existing.ProviderId != currentUserId)
        {
            throw new ValidationException("You can only update your own businesses.");
        }

        // Update business properties
        existing.BusinessName = request.BusinessName;
        existing.Description = request.Description;
        existing.Address = request.Address;
        existing.Phone = request.Phone;
        existing.Email = request.Email;
        existing.City = request.City;
        existing.Website = request.Website;
        existing.ImageUrl = request.ImageUrl;
        existing.IsActive = request.IsActive;

        await _businesses.SaveAsync(existing);
        return existing;
    }
}