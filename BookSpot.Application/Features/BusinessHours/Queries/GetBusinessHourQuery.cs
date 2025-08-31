using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.BusinessHours.Queries;

public record GetBusinessHourQuery(string Id) : IRequest<BusinessHour?>;

public class GetBusinessHourHandler : IRequestHandler<GetBusinessHourQuery, BusinessHour?>
{
    private readonly IBusinessHourRepository _businessHours;
    public GetBusinessHourHandler(IBusinessHourRepository businessHours) => _businessHours = businessHours;

    public Task<BusinessHour?> Handle(GetBusinessHourQuery request, CancellationToken cancellationToken)
        => _businessHours.GetAsync(request.Id);
}