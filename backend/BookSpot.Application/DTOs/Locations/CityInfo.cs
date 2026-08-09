namespace BookSpot.Application.DTOs.Locations;

public class CityInfo
{
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public int ServiceCount { get; set; }
    public int BusinessCount { get; set; }
    public int ProviderCount { get; set; }

    // Additional useful information
    public decimal AverageServicePrice { get; set; }
    public List<string> PopularCategories { get; set; } = new();
}