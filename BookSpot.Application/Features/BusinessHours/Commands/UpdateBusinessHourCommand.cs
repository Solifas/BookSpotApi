using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.BusinessHours.Commands;

public record UpdateBusinessHourCommand(string Id, int DayOfWeek, string OpenTime, string CloseTime, bool IsClosed) : IRequest<BusinessHour?>;

public class UpdateBusinessHourHandler : IRequestHandler<UpdateBusinessHourCommand, BusinessHour?>
{
    private readonly IBusinessHourRepository _businessHours;
    public UpdateBusinessHourHandler(IBusinessHourRepository businessHours) => _businessHours = businessHours;

    public async Task<BusinessHour?> Handle(UpdateBusinessHourCommand request, CancellationToken cancellationToken)
    {
        var existing = await _businessHours.GetAsync(request.Id);
        if (existing is null) return null;

        existing.DayOfWeek = request.DayOfWeek;
        existing.OpenTime = request.OpenTime;
        existing.CloseTime = request.CloseTime;
        existing.IsClosed = request.IsClosed;

        await _businessHours.SaveAsync(existing);
        return existing;
    }
}