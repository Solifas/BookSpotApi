using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.DTOs.Locations;
using MediatR;

namespace BookSpot.Application.Features.Locations.Queries;

public record GetAvailableCitiesQuery() : IRequest<IEnumerable<CityInfo>>;

public class GetAvailableCitiesHandler : IRequestHandler<GetAvailableCitiesQuery, IEnumerable<CityInfo>>
{
    private readonly IBusinessRepository _businesses;
    private readonly IServiceRepository _services;
    private readonly IProfileRepository _profiles;

    public GetAvailableCitiesHandler(
        IBusinessRepository businesses,
        IServiceRepository services,
        IProfileRepository profiles)
    {
        _businesses = businesses;
        _services = services;
        _profiles = profiles;
    }

    public async Task<IEnumerable<CityInfo>> Handle(GetAvailableCitiesQuery request, CancellationToken cancellationToken)
    {
        // Get all active businesses
        var allBusinesses = await _businesses.GetAllAsync();
        var activeBusinesses = allBusinesses.Where(b => b.IsActive).ToList();

        // Get all active services
        var allServices = await _services.GetAllAsync();
        var activeServices = allServices.Where(s => s.IsActive).ToList();

        // Group businesses by city
        var citiesData = activeBusinesses
            .GroupBy(b => b.City.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToList();

        var cityInfoList = new List<CityInfo>();

        foreach (var cityGroup in citiesData)
        {
            var cityName = cityGroup.Key;
            var businessesInCity = cityGroup.ToList();

            // Get services for businesses in this city
            var servicesInCity = activeServices
                .Where(s => businessesInCity.Any(b => b.Id == s.BusinessId))
                .ToList();

            // Get unique providers in this city
            var providerIds = businessesInCity.Select(b => b.ProviderId).Distinct().ToList();

            // Calculate average service price
            var averagePrice = servicesInCity.Any()
                ? servicesInCity.Average(s => s.Price)
                : 0;

            // Get popular categories (top 3)
            var popularCategories = servicesInCity
                .Where(s => !string.IsNullOrEmpty(s.Category))
                .GroupBy(s => s.Category.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();

            // For now, we'll use a simple province mapping
            // In a real application, you might have a more sophisticated location service
            var province = GetProvinceForCity(cityName);

            cityInfoList.Add(new CityInfo
            {
                City = cityName,
                Province = province,
                ServiceCount = servicesInCity.Count,
                BusinessCount = businessesInCity.Count,
                ProviderCount = providerIds.Count,
                AverageServicePrice = Math.Round(averagePrice, 2),
                PopularCategories = popularCategories
            });
        }

        return cityInfoList
            .OrderByDescending(c => c.ServiceCount)
            .ThenBy(c => c.City);
    }

    private static string GetProvinceForCity(string city)
    {
        // Simple mapping - in a real application, you'd use a proper location service
        var cityProvinceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "New York", "New York" },
            { "Los Angeles", "California" },
            { "Chicago", "Illinois" },
            { "Houston", "Texas" },
            { "Phoenix", "Arizona" },
            { "Philadelphia", "Pennsylvania" },
            { "San Antonio", "Texas" },
            { "San Diego", "California" },
            { "Dallas", "Texas" },
            { "San Jose", "California" },
            { "Austin", "Texas" },
            { "Jacksonville", "Florida" },
            { "Fort Worth", "Texas" },
            { "Columbus", "Ohio" },
            { "Charlotte", "North Carolina" },
            { "San Francisco", "California" },
            { "Indianapolis", "Indiana" },
            { "Seattle", "Washington" },
            { "Denver", "Colorado" },
            { "Boston", "Massachusetts" },
            { "Toronto", "Ontario" },
            { "Vancouver", "British Columbia" },
            { "Montreal", "Quebec" },
            { "Calgary", "Alberta" },
            { "Ottawa", "Ontario" },
            { "Edmonton", "Alberta" },
            { "Mississauga", "Ontario" },
            { "Winnipeg", "Manitoba" },
            { "Quebec City", "Quebec" },
            { "Hamilton", "Ontario" }
        };

        return cityProvinceMap.TryGetValue(city, out var province) ? province : "Unknown";
    }
}