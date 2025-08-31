using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Businesses.Queries;

public record GetBusinessQuery(string Id) : IRequest<Business?>;

public class GetBusinessHandler : IRequestHandler<GetBusinessQuery, Business?>
{
    private readonly IBusinessRepository _businesses;
    public GetBusinessHandler(IBusinessRepository businesses) => _businesses = businesses;

    public Task<Business?> Handle(GetBusinessQuery request, CancellationToken cancellationToken)
        => _businesses.GetAsync(request.Id);
}