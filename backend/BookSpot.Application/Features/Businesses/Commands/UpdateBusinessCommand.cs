using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using MediatR;

namespace BookSpot.Application.Features.Businesses.Commands;

public record UpdateBusinessCommand(
    string Id, 
    string? BusinessName = null, 
    string? Description = null, 
    string? Address = null, 
    string? Phone = null, 
    string? Email = null, 
    string? City = null, 
    string? Website = null,
    string? ImageUrl = null,
    bool? IsActive = null
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

        // Update only the fields that are provided (partial update)
        if (request.BusinessName != null)
            existing.BusinessName = request.BusinessName;
        
        if (request.Description != null)
            existing.Description = request.Description;
        
        if (request.Address != null)
            existing.Address = request.Address;
        
        if (request.Phone != null)
            existing.Phone = request.Phone;
        
        if (request.Email != null)
            existing.Email = request.Email;
        
        if (request.City != null)
            existing.City = request.City;
        
        if (request.Website != null)
            existing.Website = request.Website;
        
        if (request.ImageUrl != null)
            existing.ImageUrl = request.ImageUrl;
        
        if (request.IsActive.HasValue)
            existing.IsActive = request.IsActive.Value;

        await _businesses.SaveAsync(existing);
        return existing;
    }
}