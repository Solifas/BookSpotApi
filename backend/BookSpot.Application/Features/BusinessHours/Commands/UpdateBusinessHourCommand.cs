using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using MediatR;

namespace BookSpot.Application.Features.BusinessHours.Commands;

public record UpdateBusinessHourCommand(string Id, int DayOfWeek, string OpenTime, string CloseTime, bool IsClosed) : IRequest<BusinessHour?>;

public class UpdateBusinessHourHandler : IRequestHandler<UpdateBusinessHourCommand, BusinessHour?>
{
    private readonly IBusinessHourRepository _businessHours;
    private readonly IBusinessRepository _businesses;
    private readonly IClaimsService _claims;
    public UpdateBusinessHourHandler(IBusinessHourRepository businessHours, IBusinessRepository businesses, IClaimsService claims)
    {
        _businessHours = businessHours;
        _businesses = businesses;
        _claims = claims;
    }

    public async Task<BusinessHour?> Handle(UpdateBusinessHourCommand request, CancellationToken cancellationToken)
    {
        var existing = await _businessHours.GetAsync(request.Id);
        if (existing is null) return null;
        var business = await _businesses.GetAsync(existing.BusinessId);
        if (business is null || !_claims.IsProvider() ||
            !string.Equals(business.ProviderId, _claims.GetCurrentUserId(), StringComparison.Ordinal)) return null;

        existing.DayOfWeek = request.DayOfWeek;
        existing.OpenTime = request.OpenTime;
        existing.CloseTime = request.CloseTime;
        existing.IsClosed = request.IsClosed;

        await _businessHours.SaveAsync(existing);
        return existing;
    }
}