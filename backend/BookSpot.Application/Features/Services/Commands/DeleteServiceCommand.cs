using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using MediatR;

namespace BookSpot.Application.Features.Services.Commands;

public record DeleteServiceCommand(string Id) : IRequest<bool>;

public class DeleteServiceHandler : IRequestHandler<DeleteServiceCommand, bool>
{
    private readonly IServiceRepository _services;
    private readonly IBusinessRepository _businesses;
    private readonly IClaimsService _claims;
    public DeleteServiceHandler(IServiceRepository services, IBusinessRepository businesses, IClaimsService claims)
    {
        _services = services;
        _businesses = businesses;
        _claims = claims;
    }

    public async Task<bool> Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _services.GetAsync(request.Id);
        if (existing is null) return false;
        var business = await _businesses.GetAsync(existing.BusinessId);
        if (business is null || !_claims.IsProvider() ||
            !string.Equals(business.ProviderId, _claims.GetCurrentUserId(), StringComparison.Ordinal)) return false;
        await _services.DeleteAsync(request.Id);
        return true;
    }
}