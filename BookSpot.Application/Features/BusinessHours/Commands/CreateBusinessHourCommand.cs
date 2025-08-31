using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.BusinessHours.Commands;

public record CreateBusinessHourCommand(string BusinessId, int DayOfWeek, string OpenTime, string CloseTime, bool IsClosed) : IRequest<BusinessHour>;

public class CreateBusinessHourHandler : IRequestHandler<CreateBusinessHourCommand, BusinessHour>
{
    private readonly IBusinessHourRepository _businessHours;
    public CreateBusinessHourHandler(IBusinessHourRepository businessHours) => _businessHours = businessHours;

    public async Task<BusinessHour> Handle(CreateBusinessHourCommand request, CancellationToken cancellationToken)
    {
        var businessHour = new BusinessHour
        {
            Id = Guid.NewGuid().ToString(),
            BusinessId = request.BusinessId,
            DayOfWeek = request.DayOfWeek,
            OpenTime = request.OpenTime,
            CloseTime = request.CloseTime,
            IsClosed = request.IsClosed
        };

        await _businessHours.SaveAsync(businessHour);
        return businessHour;
    }
}