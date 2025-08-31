using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Businesses.Commands;

public record CreateBusinessCommand(string ProviderId, string BusinessName, string City) : IRequest<Business>;

public class CreateBusinessHandler : IRequestHandler<CreateBusinessCommand, Business>
{
    private readonly IBusinessRepository _businesses;
    public CreateBusinessHandler(IBusinessRepository businesses) => _businesses = businesses;

    public async Task<Business> Handle(CreateBusinessCommand request, CancellationToken cancellationToken)
    {
        var business = new Business
        {
            Id = Guid.NewGuid().ToString(),
            ProviderId = request.ProviderId,
            BusinessName = request.BusinessName,
            City = request.City,
            CreatedAt = DateTime.UtcNow
        };

        await _businesses.SaveAsync(business);
        return business;
    }
}