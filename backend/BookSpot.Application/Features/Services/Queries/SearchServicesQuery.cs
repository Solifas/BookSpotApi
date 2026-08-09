using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Domain.Entities;
using MediatR;

namespace BookSpot.Application.Features.Services.Queries;

public record SearchServicesQuery(
    string? Name = null,
    string? City = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? MinDuration = null,
    int? MaxDuration = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<IEnumerable<Service>>;

public class SearchServicesHandler : IRequestHandler<SearchServicesQuery, IEnumerable<Service>>
{
    private readonly IServiceRepository _services;
    private readonly IBusinessRepository _businesses;

    public SearchServicesHandler(IServiceRepository services, IBusinessRepository businesses)
    {
        _services = services;
        _businesses = businesses;
    }

    public async Task<IEnumerable<Service>> Handle(SearchServicesQuery request, CancellationToken cancellationToken)
    {
        // Get all services first
        var allServices = await _services.GetAllAsync();

        // Apply filters
        var filteredServices = allServices.AsEnumerable();

        if (!string.IsNullOrEmpty(request.Name))
        {
            filteredServices = filteredServices.Where(s =>
                s.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
        }

        if (request.MinPrice.HasValue)
        {
            filteredServices = filteredServices.Where(s => s.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            filteredServices = filteredServices.Where(s => s.Price <= request.MaxPrice.Value);
        }

        if (request.MinDuration.HasValue)
        {
            filteredServices = filteredServices.Where(s => s.DurationMinutes >= request.MinDuration.Value);
        }

        if (request.MaxDuration.HasValue)
        {
            filteredServices = filteredServices.Where(s => s.DurationMinutes <= request.MaxDuration.Value);
        }

        // Filter by city if specified (requires business lookup)
        if (!string.IsNullOrEmpty(request.City))
        {
            var businessIds = new List<string>();

            // This is not optimal for large datasets, but works for the current implementation
            // In a real-world scenario, you'd want to implement proper database-level filtering
            foreach (var service in filteredServices)
            {
                var business = await _businesses.GetAsync(service.BusinessId);
                if (business != null && business.City.Contains(request.City, StringComparison.OrdinalIgnoreCase))
                {
                    businessIds.Add(service.BusinessId);
                }
            }

            filteredServices = filteredServices.Where(s => businessIds.Contains(s.BusinessId));
        }

        // Apply pagination
        var skip = (request.Page - 1) * request.PageSize;
        return filteredServices.Skip(skip).Take(request.PageSize);
    }
}