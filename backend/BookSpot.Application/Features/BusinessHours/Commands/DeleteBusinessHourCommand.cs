using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using MediatR;

namespace BookSpot.Application.Features.BusinessHours.Commands;

public record DeleteBusinessHourCommand(string Id) : IRequest<bool>;

public class DeleteBusinessHourHandler : IRequestHandler<DeleteBusinessHourCommand, bool>
{
    private readonly IBusinessHourRepository _businessHours;
    private readonly IBusinessRepository _businesses;
    private readonly IClaimsService _claims;
    public DeleteBusinessHourHandler(IBusinessHourRepository businessHours, IBusinessRepository businesses, IClaimsService claims)
    {
        _businessHours = businessHours;
        _businesses = businesses;
        _claims = claims;
    }

    public async Task<bool> Handle(DeleteBusinessHourCommand request, CancellationToken cancellationToken)
    {
        var existing = await _businessHours.GetAsync(request.Id);
        if (existing is null) return false;
        var business = await _businesses.GetAsync(existing.BusinessId);
        if (business is null || !_claims.IsProvider() ||
            !string.Equals(business.ProviderId, _claims.GetCurrentUserId(), StringComparison.Ordinal)) return false;
        await _businessHours.DeleteAsync(request.Id);
        return true;
    }
}