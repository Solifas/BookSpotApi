using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;
using MediatR;

namespace BookSpot.Application.Features.Businesses.Commands;

public record CreateBusinessCommand(
    string BusinessName,
    string Description,
    string Address,
    string Phone,
    string Email,
    string City,
    string? Website = null,
    string? ImageUrl = null,
    bool IsActive = true
) : IRequest<Business>;

public class CreateBusinessHandler : IRequestHandler<CreateBusinessCommand, Business>
{
    private readonly IBusinessRepository _businesses;
    private readonly IProfileRepository _profiles;
    private readonly IClaimsService _claimsService;

    public CreateBusinessHandler(IBusinessRepository businesses, IProfileRepository profiles, IClaimsService claimsService)
    {
        _businesses = businesses;
        _profiles = profiles;
        _claimsService = claimsService;
    }

    public async Task<Business> Handle(CreateBusinessCommand request, CancellationToken cancellationToken)
    {
        // Get current user from JWT claims
        var currentUserId = _claimsService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new ValidationException("User must be authenticated to create a business.");
        }

        // Validate that the current user is a provider
        if (!_claimsService.IsProvider())
        {
            throw new ValidationException("Only providers can create businesses.");
        }

        // Validate that the profile exists (additional security check)
        var profile = await _profiles.GetAsync(currentUserId);
        if (profile == null)
        {
            throw new NotFoundException($"Current user profile not found.");
        }

        var business = new Business
        {
            Id = Guid.NewGuid().ToString(),
            ProviderId = currentUserId,
            BusinessName = request.BusinessName,
            Description = request.Description,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            City = request.City,
            Website = request.Website,
            ImageUrl = request.ImageUrl,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _businesses.SaveAsync(business);
        return business;
    }
}