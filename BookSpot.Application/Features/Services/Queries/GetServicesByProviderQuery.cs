using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;
using MediatR;

namespace BookSpot.Application.Features.Services.Queries;

public record GetServicesByProviderQuery(string ProviderId) : IRequest<IEnumerable<Service>>;

public class GetServicesByProviderHandler : IRequestHandler<GetServicesByProviderQuery, IEnumerable<Service>>
{
    private readonly IServiceRepository _services;
    private readonly IBusinessRepository _businesses;
    private readonly IProfileRepository _profiles;

    public GetServicesByProviderHandler(
        IServiceRepository services,
        IBusinessRepository businesses,
        IProfileRepository profiles)
    {
        _services = services;
        _businesses = businesses;
        _profiles = profiles;
    }

    public async Task<IEnumerable<Service>> Handle(GetServicesByProviderQuery request, CancellationToken cancellationToken)
    {
        // Verify provider exists
        var provider = await _profiles.GetAsync(request.ProviderId);
        if (provider == null)
        {
            throw new NotFoundException($"Provider with ID '{request.ProviderId}' not found.");
        }

        if (provider.UserType != "provider")
        {
            throw new ValidationException($"User with ID '{request.ProviderId}' is not a provider.");
        }

        // Get all businesses owned by this provider
        var allBusinesses = await _businesses.GetAllAsync();
        var providerBusinesses = allBusinesses.Where(b => b.ProviderId == request.ProviderId);

        // Get all services for these businesses
        var allServices = await _services.GetAllAsync();
        var providerServices = allServices.Where(s =>
            providerBusinesses.Any(b => b.Id == s.BusinessId));

        return providerServices.OrderBy(s => s.Name);
    }
}