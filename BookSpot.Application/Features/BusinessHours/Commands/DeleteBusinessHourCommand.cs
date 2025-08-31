using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.BusinessHours.Commands;

public record DeleteBusinessHourCommand(string Id) : IRequest<bool>;

public class DeleteBusinessHourHandler : IRequestHandler<DeleteBusinessHourCommand, bool>
{
    private readonly IBusinessHourRepository _businessHours;
    public DeleteBusinessHourHandler(IBusinessHourRepository businessHours) => _businessHours = businessHours;

    public async Task<bool> Handle(DeleteBusinessHourCommand request, CancellationToken cancellationToken)
    {
        var existing = await _businessHours.GetAsync(request.Id);
        if (existing is null) return false;
        await _businessHours.DeleteAsync(request.Id);
        return true;
    }
}