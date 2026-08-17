using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.DTOs.Canonical;
using BookSpot.Application.Exceptions;
using MediatR;

namespace BookSpot.Application.Features.Services.Queries;

public record SearchServicesQuery(string? Name = null, string? Category = null, string? City = null,
    decimal? MinPrice = null, decimal? MaxPrice = null, int? MinDuration = null, int? MaxDuration = null,
    int Page = 1, int PageSize = 20) : IRequest<ServiceSearchResponse>;

public class SearchServicesHandler(IServiceRepository services, IBusinessRepository businesses,
    IProfileRepository profiles) : IRequestHandler<SearchServicesQuery, ServiceSearchResponse>
{
    public async Task<ServiceSearchResponse> Handle(SearchServicesQuery request, CancellationToken cancellationToken)
    {
        if (request.Page is < 1 or > 100000 || request.PageSize is < 1 or > 100)
            throw new ValidationException("Invalid pagination values.");
        var source = (await services.GetAllAsync()).Where(service => service.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Name))
            source = source.Where(service => service.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(request.Category))
            source = source.Where(service => string.Equals(service.Category, request.Category, StringComparison.OrdinalIgnoreCase));
        if (request.MinPrice.HasValue) source = source.Where(service => service.Price >= request.MinPrice.Value);
        if (request.MaxPrice.HasValue) source = source.Where(service => service.Price <= request.MaxPrice.Value);
        if (request.MinDuration.HasValue) source = source.Where(service => service.DurationMinutes >= request.MinDuration.Value);
        if (request.MaxDuration.HasValue) source = source.Where(service => service.DurationMinutes <= request.MaxDuration.Value);

        var projected = new List<ServiceDto>();
        foreach (var service in source)
        {
            var business = await businesses.GetAsync(service.BusinessId);
            if (business is null || !business.IsActive ||
                (!string.IsNullOrWhiteSpace(request.City) &&
                 !business.City.Contains(request.City, StringComparison.OrdinalIgnoreCase))) continue;
            var profile = await profiles.GetAsync(business.ProviderId);
            projected.Add(CanonicalDtoMapper.ToServiceDto(service, business, profile?.FullName ?? service.ProviderName));
        }

        var totalCount = projected.Count;
        var items = projected.OrderBy(value => value.ServiceId, StringComparer.Ordinal)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToArray();
        return new ServiceSearchResponse(items, totalCount, request.Page, request.PageSize);
    }
}
