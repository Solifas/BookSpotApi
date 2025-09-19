using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;
using MediatR;

namespace BookSpot.Application.Features.Services.Queries;

public record GetServicesByBusinessQuery(string BusinessId) : IRequest<IEnumerable<Service>>;

public class GetServicesByBusinessHandler : IRequestHandler<GetServicesByBusinessQuery, IEnumerable<Service>>
{
    private readonly IServiceRepository _services;
    private readonly IBusinessRepository _businesses;
    private readonly IClaimsService _claimsService;

    public GetServicesByBusinessHandler(
        IServiceRepository services,
        IBusinessRepository businesses,
        IClaimsService claimsService)
    {
        _services = services;
        _businesses = businesses;
        _claimsService = claimsService;
    }

    public async Task<IEnumerable<Service>> Handle(GetServicesByBusinessQuery request, CancellationToken cancellationToken)
    {
        // Get current user ID from claims
        var currentUserId = _claimsService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new ValidationException("User not authenticated.");
        }

        // Validate that the user owns this business
        if (request.BusinessId != currentUserId)
        {
            throw new ValidationException($"Access denied. You can only view services for your own businesses.");
        }

        var allServices = await _services.GetAllAsync();
        return allServices.Where(s => s.BusinessId == request.BusinessId);
    }
}