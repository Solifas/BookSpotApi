using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Businesses.Commands;

public record UpdateBusinessCommand(string Id, string BusinessName, string City, bool IsActive) : IRequest<Business?>;

public class UpdateBusinessHandler : IRequestHandler<UpdateBusinessCommand, Business?>
{
    private readonly IBusinessRepository _businesses;
    public UpdateBusinessHandler(IBusinessRepository businesses) => _businesses = businesses;

    public async Task<Business?> Handle(UpdateBusinessCommand request, CancellationToken cancellationToken)
    {
        var existing = await _businesses.GetAsync(request.Id);
        if (existing is null) return null;

        existing.BusinessName = request.BusinessName;
        existing.City = request.City;
        existing.IsActive = request.IsActive;

        await _businesses.SaveAsync(existing);
        return existing;
    }
}