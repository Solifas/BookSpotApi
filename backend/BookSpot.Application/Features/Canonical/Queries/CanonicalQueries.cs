using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.DTOs.Canonical;
using BookSpot.Application.Exceptions;
using BookSpot.Application.Features.Availability;
using MediatR;

namespace BookSpot.Application.Features.Canonical.Queries;

public sealed record GetMyBusinessesQuery(string ProviderProfileId) : IRequest<IReadOnlyList<BusinessDto>>;
public sealed class GetMyBusinessesHandler(IBusinessRepository businesses)
    : IRequestHandler<GetMyBusinessesQuery, IReadOnlyList<BusinessDto>>
{
    public async Task<IReadOnlyList<BusinessDto>> Handle(GetMyBusinessesQuery request, CancellationToken cancellationToken) =>
        (await businesses.GetAllAsync()).Where(value => value.ProviderId == request.ProviderProfileId)
            .OrderBy(value => value.CreatedAt).Select(CanonicalDtoMapper.ToBusinessDto).ToArray();
}

public sealed record GetCanonicalBookingPageQuery(string ProfileId, string View, string? Status = null,
    DateTimeOffset? From = null, DateTimeOffset? To = null, string? BusinessId = null) : IRequest<BookingPageDto>;
public sealed class GetCanonicalBookingPageHandler(IBookingRepository bookings, IServiceRepository services,
    IBusinessRepository businesses, IProfileRepository profiles)
    : IRequestHandler<GetCanonicalBookingPageQuery, BookingPageDto>
{
    public async Task<BookingPageDto> Handle(GetCanonicalBookingPageQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<Domain.Entities.Booking> source;
        if (request.View == "client")
        {
            source = await bookings.GetBookingsByClientAsync(request.ProfileId);
        }
        else
        {
            var ownedBusinesses = (await businesses.GetAllAsync())
                .Where(value => value.ProviderId == request.ProfileId).ToArray();
            if (request.BusinessId is not null && ownedBusinesses.All(value => value.Id != request.BusinessId))
                throw new NotFoundException("Business", request.BusinessId);
            source = request.BusinessId is null
                ? await bookings.GetBookingsByProviderAsync(request.ProfileId)
                : await bookings.GetBookingsByBusinessAsync(request.BusinessId);
        }
        if (request.Status is not null) source = source.Where(value => value.Status == request.Status);
        if (request.From is not null) source = source.Where(value => value.StartTime >= request.From.Value.UtcDateTime);
        if (request.To is not null) source = source.Where(value => value.StartTime < request.To.Value.UtcDateTime);


        var result = new List<BookingDto>();
        foreach (var booking in source.OrderByDescending(value => value.StartTime))
        {
            var service = await services.GetAsync(booking.ServiceId);
            var business = await businesses.GetAsync(booking.BusinessId);
            var client = await profiles.GetAsync(booking.ClientId);
            if (request.View == "provider" && booking.ProviderProfileId != request.ProfileId) continue;
            try
            {
                result.Add(CanonicalDtoMapper.ToBookingDto(booking, service, business, client, request.View));
            }
            catch (InvalidOperationException)
            {
                // Legacy rows without snapshots are projectable only while their live related records remain.
            }
        }
        return new BookingPageDto(result);
    }
}

public sealed record GetServiceAvailabilityQuery(string ServiceId, DateTimeOffset From, DateTimeOffset To)
    : IRequest<ServiceAvailabilityDto>;
public sealed class GetServiceAvailabilityHandler(IServiceRepository services, IBusinessRepository businesses,
    IBusinessHourRepository hours, IBookingRepository bookings)
    : IRequestHandler<GetServiceAvailabilityQuery, ServiceAvailabilityDto>
{
    public async Task<ServiceAvailabilityDto> Handle(GetServiceAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var service = await services.GetAsync(request.ServiceId);
        if (service is null || !service.IsActive) throw new NotFoundException("Service", request.ServiceId);
        var business = await businesses.GetAsync(service.BusinessId);
        if (business is null || !business.IsActive) throw new NotFoundException("Service", request.ServiceId);
        var schedule = await hours.GetByBusinessAsync(business.Id);
        var conflicts = await bookings.GetBookingsByBusinessAsync(business.Id);
        return AvailabilityCalculator.Calculate(service, schedule,
            conflicts, request.From, request.To,
            string.IsNullOrWhiteSpace(business.TimeZone) ? CanonicalDtoMapper.DefaultTimeZone : business.TimeZone);
    }
}

public sealed record GetCanonicalDashboardQuery(string ProfileId, string Role, DateTime GeneratedAt)
    : IRequest<DashboardDto>;
public sealed class GetCanonicalDashboardHandler(IBookingRepository bookings, IServiceRepository services,
    IBusinessRepository businesses, IProfileRepository profiles)
    : IRequestHandler<GetCanonicalDashboardQuery, DashboardDto>
{
    public async Task<DashboardDto> Handle(GetCanonicalDashboardQuery request, CancellationToken cancellationToken)
    {
        var pageHandler = new GetCanonicalBookingPageHandler(bookings, services, businesses, profiles);
        var view = request.Role == "provider" ? "provider" : "client";
        var page = await pageHandler.Handle(new GetCanonicalBookingPageQuery(request.ProfileId, view), cancellationToken);
        var now = DateTime.SpecifyKind(request.GeneratedAt, DateTimeKind.Utc);
        if (view == "client")
        {
            var source = await bookings.GetBookingsByClientAsync(request.ProfileId);
            var rows = source.ToArray();
            var totalSpent = await SumCapturedPrices(rows.Where(value => value.Status == "completed"));
            return new ClientDashboardDto(now, rows.Length, rows.Count(value => value.Status == "completed"),
                rows.Count(value => value.Status == "cancelled"), rows.Count(value => value.Status == "pending"),
                new MoneyDto(totalSpent),
                page.Items.Where(value => value.StartTime >= now && value.Status is "pending" or "confirmed").ToArray(),
                page.Items.OrderByDescending(value => value.UpdatedAt).Take(10).ToArray());
        }

        var providerBusinesses = (await businesses.GetAllAsync()).Where(value => value.ProviderId == request.ProfileId).ToArray();
        var providerServices = (await services.GetAllAsync()).Where(value =>
            providerBusinesses.Any(business => business.Id == value.BusinessId) && value.IsActive).ToArray();
        var rowsProvider = new List<Domain.Entities.Booking>();
        foreach (var business in providerBusinesses)
            rowsProvider.AddRange(await bookings.GetBookingsByBusinessAsync(business.Id));
        var zoneName = providerBusinesses.Select(value => value.TimeZone).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? CanonicalDtoMapper.DefaultTimeZone;
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneName);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, zone);
        var clients = new List<RecentClientDto>();
        foreach (var group in rowsProvider.GroupBy(value => value.ClientId))
        {
            var profile = await profiles.GetAsync(group.Key);
            if (profile is not null) clients.Add(new RecentClientDto(profile.Id, profile.FullName,
                group.Max(value => value.StartTime), group.Count()));
        }
        var monthlyRevenue = await SumCapturedPrices(rowsProvider.Where(value =>
            value.Status == "completed" && value.StartTime >= now.AddDays(-30)));
        return new ProviderDashboardDto(now, zoneName,
            rowsProvider.Count(value => TimeZoneInfo.ConvertTimeFromUtc(value.StartTime, zone).Date == localNow.Date),
            rowsProvider.Count(value => value.StartTime >= now.AddDays(-7) && value.StartTime < now),
            rowsProvider.Count(value => value.Status == "pending"), rowsProvider.Select(value => value.ClientId).Distinct().Count(),
            providerServices.Length,
            new MoneyDto(monthlyRevenue),
            page.Items.Where(value => value.StartTime >= now && value.Status is "pending" or "confirmed").ToArray(),
            clients.OrderByDescending(value => value.LastBookingAt).Take(100).ToArray());
    }

    private async Task<decimal> SumCapturedPrices(IEnumerable<Domain.Entities.Booking> source)
    {
        decimal total = 0;
        foreach (var booking in source)
            total += booking.PriceAmount ?? (await services.GetAsync(booking.ServiceId))?.Price ?? 0m;
        return total;
    }
}
