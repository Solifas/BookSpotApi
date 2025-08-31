using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Services.Queries;

public record GetServiceQuery(string Id) : IRequest<Service?>;

public class GetServiceHandler : IRequestHandler<GetServiceQuery, Service?>
{
    private readonly IServiceRepository _services;
    public GetServiceHandler(IServiceRepository services) => _services = services;

    public Task<Service?> Handle(GetServiceQuery request, CancellationToken cancellationToken)
        => _services.GetAsync(request.Id);
}