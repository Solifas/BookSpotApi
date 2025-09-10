using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Services.Queries;

public record GetAllServicesQuery() : IRequest<IEnumerable<Service>>;

public class GetAllServicesHandler : IRequestHandler<GetAllServicesQuery, IEnumerable<Service>>
{
    private readonly IServiceRepository _services;
    public GetAllServicesHandler(IServiceRepository services) => _services = services;

    public Task<IEnumerable<Service>> Handle(GetAllServicesQuery request, CancellationToken cancellationToken)
        => _services.GetAllAsync();
}