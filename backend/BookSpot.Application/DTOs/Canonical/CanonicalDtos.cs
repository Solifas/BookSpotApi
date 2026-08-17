using BookSpot.Domain.Entities;
using System.Text.Json.Serialization;

namespace BookSpot.Application.DTOs.Canonical;

public sealed record MoneyDto(decimal Amount, string Currency = "ZAR");
public sealed record BusinessDto(string BusinessId, string ProviderProfileId, string BusinessName, string Description,
    string Address, string City, string Phone, string Email, string? Website, string? ImageUrl, bool IsActive,
    double Rating, int ReviewCount, string TimeZone, DateTime CreatedAt);
public sealed record ServiceDto(string ServiceId, string BusinessId, string ProviderProfileId, string ProviderDisplayName,
    string Name, string Description, string? Category, MoneyDto Price, int DurationMinutes, string? ImageUrl,
    IReadOnlyList<string> Tags, string? Location, bool IsActive, DateTime CreatedAt);
public sealed record ServiceSearchResponse(IReadOnlyList<ServiceDto> Items, int TotalCount, int Page, int PageSize);
public sealed record AvailabilitySlotDto(DateTimeOffset StartTime, DateTimeOffset EndTime);
public sealed record ServiceAvailabilityDto(string ServiceId, string BusinessId, string TimeZone, DateTimeOffset From,
    DateTimeOffset To, int DurationMinutes, IReadOnlyList<AvailabilitySlotDto> Slots);
public sealed record BookingServiceDto(string Name, int DurationMinutes);
public sealed record BookingBusinessDto(string BusinessName, string Address, string City);
public sealed record BookingClientDto(string FullName, string Email, string? ContactNumber);
[JsonPolymorphic(TypeDiscriminatorPropertyName = "view")]
[JsonDerivedType(typeof(ClientBookingDto), "client")]
[JsonDerivedType(typeof(ProviderBookingDto), "provider")]
public abstract record BookingDto(string BookingId, string ServiceId, string BusinessId,
    string ProviderProfileId, string Status, DateTime StartTime, DateTime EndTime, MoneyDto Price, int Version,
    DateTime CreatedAt, DateTime UpdatedAt, BookingServiceDto Service, BookingBusinessDto Business);
public sealed record ClientBookingDto(string BookingId, string ServiceId, string BusinessId,
    string ProviderProfileId, string Status, DateTime StartTime, DateTime EndTime, MoneyDto Price, int Version,
    DateTime CreatedAt, DateTime UpdatedAt, BookingServiceDto Service, BookingBusinessDto Business,
    string ClientProfileId) : BookingDto(BookingId, ServiceId, BusinessId, ProviderProfileId, Status, StartTime,
    EndTime, Price, Version, CreatedAt, UpdatedAt, Service, Business);
public sealed record ProviderBookingDto(string BookingId, string ServiceId, string BusinessId,
    string ProviderProfileId, string Status, DateTime StartTime, DateTime EndTime, MoneyDto Price, int Version,
    DateTime CreatedAt, DateTime UpdatedAt, BookingServiceDto Service, BookingBusinessDto Business,
    BookingClientDto Client) : BookingDto(BookingId, ServiceId, BusinessId, ProviderProfileId, Status, StartTime,
    EndTime, Price, Version, CreatedAt, UpdatedAt, Service, Business);
public sealed record BookingPageDto(IReadOnlyList<BookingDto> Items, string? NextCursor = null);
public sealed record RecentClientDto(string ClientProfileId, string FullName, DateTime LastBookingAt, int TotalBookings);
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ProviderDashboardDto), "provider")]
[JsonDerivedType(typeof(ClientDashboardDto), "client")]
public abstract record DashboardDto;
public sealed record ProviderDashboardDto(DateTime GeneratedAt, string TimeZone, int TodayBookings,
    int WeekBookings, int PendingRequests, int TotalClients, int ActiveServices, MoneyDto MonthlyRevenue,
    IReadOnlyList<BookingDto> Upcoming, IReadOnlyList<RecentClientDto> RecentClients) : DashboardDto;
public sealed record ClientDashboardDto(DateTime GeneratedAt, int TotalBookings, int CompletedBookings,
    int CancelledBookings, int PendingRequests, MoneyDto TotalSpent, IReadOnlyList<BookingDto> Upcoming,
    IReadOnlyList<BookingDto> Recent) : DashboardDto;
public sealed record ReviewDto(string ReviewId, int Rating, string Comment, DateTime CreatedAt, DateTime? UpdatedAt,
    string? DisplayName);

public sealed class CreateBusinessRequest
{
    public string BusinessName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Website { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class CreateServiceRequest
{
    public string BusinessId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Category { get; init; }
    public decimal PriceAmount { get; init; }
    public int DurationMinutes { get; init; }
    public string? ImageUrl { get; init; }
    public List<string>? Tags { get; init; }
    public string? Location { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class UpdateBusinessRequest
{
    public string? BusinessName { get; init; }
    public string? Description { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? ImageUrl { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class UpdateServiceRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public decimal? PriceAmount { get; init; }
    public int? DurationMinutes { get; init; }
    public string? ImageUrl { get; init; }
    public List<string>? Tags { get; init; }
    public string? Location { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class UpdateReviewRequest
{
    public int? Rating { get; init; }
    public string? Comment { get; init; }
}

public static class CanonicalDtoMapper
{
    public const string DefaultTimeZone = "Africa/Johannesburg";

    public static BusinessDto ToBusinessDto(Business value) => new(value.Id, value.ProviderId, value.BusinessName,
        value.Description, value.Address, value.City, value.Phone, value.Email, value.Website, value.ImageUrl,
        value.IsActive, value.Rating, value.ReviewCount,
        string.IsNullOrWhiteSpace(value.TimeZone) ? DefaultTimeZone : value.TimeZone, AsUtc(value.CreatedAt));

    public static ServiceDto ToServiceDto(Service value, Business business, string providerDisplayName) => new(
        value.Id, business.Id, business.ProviderId, providerDisplayName, value.Name, value.Description, value.Category,
        new MoneyDto(value.Price), value.DurationMinutes, value.ImageUrl, value.Tags, value.Location, value.IsActive,
        AsUtc(value.CreatedAt));

    public static BookingDto ToBookingDto(Booking booking, Service? service, Business? business, Profile? client,
        string view)
    {
        var startTime = AsUtc(booking.StartTime);
        var endTime = AsUtc(booking.EndTime);
        var createdAt = AsUtc(booking.CreatedAt);
        var updatedAt = AsUtc(booking.UpdatedAt);
        var price = new MoneyDto(booking.PriceAmount ?? service?.Price ??
            throw new InvalidOperationException("Legacy booking is missing a price snapshot."));
        var serviceDto = new BookingServiceDto(booking.ServiceNameSnapshot ?? service?.Name ??
            throw new InvalidOperationException("Legacy booking is missing a service snapshot."),
            booking.DurationMinutesSnapshot ?? service?.DurationMinutes ??
            throw new InvalidOperationException("Legacy booking is missing a duration snapshot."));
        var businessDto = new BookingBusinessDto(booking.BusinessNameSnapshot ?? business?.BusinessName ??
            throw new InvalidOperationException("Legacy booking is missing a business snapshot."),
            booking.BusinessAddressSnapshot ?? business?.Address ??
            throw new InvalidOperationException("Legacy booking is missing a business address snapshot."),
            booking.BusinessCitySnapshot ?? business?.City ??
            throw new InvalidOperationException("Legacy booking is missing a business city snapshot."));
        var providerProfileId = string.IsNullOrEmpty(booking.ProviderProfileId)
            ? business?.ProviderId ?? booking.ProviderId
            : booking.ProviderProfileId;
        return view == "client"
            ? new ClientBookingDto(booking.Id, booking.ServiceId, booking.BusinessId, providerProfileId, booking.Status,
                startTime, endTime, price, booking.Version, createdAt, updatedAt, serviceDto, businessDto,
                booking.ClientId)
            : new ProviderBookingDto(booking.Id, booking.ServiceId, booking.BusinessId, providerProfileId, booking.Status,
                startTime, endTime, price, booking.Version, createdAt, updatedAt, serviceDto, businessDto,
                new BookingClientDto(booking.ClientFullNameSnapshot ?? client?.FullName ??
                    throw new InvalidOperationException("Legacy booking is missing a client name snapshot."),
                    booking.ClientEmailSnapshot ?? client?.Email ??
                    throw new InvalidOperationException("Legacy booking is missing a client email snapshot."),
                    booking.ClientPhoneSnapshot ?? client?.ContactNumber));
    }

    public static ReviewDto ToReviewDto(Review value, string? displayName = null) =>
        new(value.Id, value.Rating, value.Comment, AsUtc(value.CreatedAt), value.UpdatedAt is null ? null : AsUtc(value.UpdatedAt.Value), displayName);

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
