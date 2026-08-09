using System.Collections.Generic;
using System.Linq;

using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.DTOs.Dashboard;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;
using MediatR;

namespace BookSpot.Application.Features.Dashboard.Queries;

public record GetProviderInsightsQuery(string ProviderId, DateTime? StartDateUtc, DateTime? EndDateUtc) : IRequest<ProviderInsightsResponse>;

public class GetProviderInsightsHandler : IRequestHandler<GetProviderInsightsQuery, ProviderInsightsResponse>
{
    private readonly IBookingRepository _bookings;
    private readonly IServiceRepository _services;
    private readonly IProfileRepository _profiles;

    public GetProviderInsightsHandler(
        IBookingRepository bookings,
        IServiceRepository services,
        IProfileRepository profiles)
    {
        _bookings = bookings;
        _services = services;
        _profiles = profiles;
    }

    public async Task<ProviderInsightsResponse> Handle(GetProviderInsightsQuery request, CancellationToken cancellationToken)
    {
        var provider = await _profiles.GetAsync(request.ProviderId);
        if (provider is null)
        {
            throw new NotFoundException($"Provider with ID '{request.ProviderId}' not found.");
        }

        if (!string.Equals(provider.UserType, "provider", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException($"User with ID '{request.ProviderId}' is not a provider.");
        }

        var bookings = await _bookings.GetBookingsByProviderAsync(request.ProviderId);
        var bookingsList = bookings.ToList();

        var startFilter = NormalizeStartDate(request.StartDateUtc);
        var endFilter = NormalizeEndDate(request.EndDateUtc);

        if (startFilter.HasValue)
        {
            bookingsList = bookingsList.Where(b => b.StartTime >= startFilter.Value).ToList();
        }

        if (endFilter.HasValue)
        {
            bookingsList = bookingsList.Where(b => b.StartTime <= endFilter.Value).ToList();
        }

        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekThreshold = now.AddDays(-7);
        var monthThreshold = now.AddDays(-30);

        var todayBookings = bookingsList.Count(b => b.StartTime.Date == today);
        var weekBookings = bookingsList.Count(b => b.StartTime >= weekThreshold && b.StartTime <= now);
        var totalClients = bookingsList.Select(b => b.ClientId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().Count();
        var pendingBookings = bookingsList.Count(b => string.Equals(b.Status, "pending", StringComparison.OrdinalIgnoreCase));
        var confirmedBookings = bookingsList.Count(b => string.Equals(b.Status, "confirmed", StringComparison.OrdinalIgnoreCase));

        var monthlyRevenue = await CalculateMonthlyRevenueAsync(bookingsList, monthThreshold, now);
        var popularServices = await CalculatePopularServicesAsync(bookingsList);

        var stats = new ProviderInsightsStats
        {
            TodayBookings = todayBookings,
            WeekBookings = weekBookings,
            TotalClients = totalClients,
            MonthlyRevenue = monthlyRevenue,
            PendingBookings = pendingBookings,
            ConfirmedBookings = confirmedBookings
        };

        return new ProviderInsightsResponse
        {
            Stats = stats,
            PopularServices = popularServices
        };
    }

    private static DateTime? NormalizeStartDate(DateTime? input)
    {
        if (!input.HasValue)
        {
            return null;
        }

        var value = input.Value;
        if (value.Kind == DateTimeKind.Unspecified)
        {
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        return value;
    }

    private static DateTime? NormalizeEndDate(DateTime? input)
    {
        if (!input.HasValue)
        {
            return null;
        }

        var value = input.Value;
        if (value.Kind == DateTimeKind.Unspecified)
        {
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        if (value.TimeOfDay == TimeSpan.Zero)
        {
            value = value.Date.AddDays(1).AddTicks(-1);
        }

        return value;
    }

    private async Task<decimal> CalculateMonthlyRevenueAsync(
        IEnumerable<Booking> bookings,
        DateTime monthThreshold,
        DateTime now)
    {
        var revenue = 0m;
        var serviceCache = new Dictionary<string, Service?>();

        foreach (var booking in bookings)
        {
            if (!string.Equals(booking.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (booking.StartTime < monthThreshold || booking.StartTime > now)
            {
                continue;
            }

            var service = await GetServiceAsync(booking.ServiceId, serviceCache);
            if (service != null)
            {
                revenue += service.Price;
            }
        }

        return revenue;
    }

    private async Task<List<PopularServiceInsight>> CalculatePopularServicesAsync(IEnumerable<Booking> bookings)
    {
        var serviceCache = new Dictionary<string, Service?>();
        var grouped = bookings.GroupBy(b => b.ServiceId);
        var popular = new List<PopularServiceInsight>();

        foreach (var group in grouped)
        {
            var service = await GetServiceAsync(group.Key, serviceCache);
            var completedBookings = group.Where(b => string.Equals(b.Status, "completed", StringComparison.OrdinalIgnoreCase));

            var revenue = 0m;
            foreach (var booking in completedBookings)
            {
                var completedService = service ?? await GetServiceAsync(booking.ServiceId, serviceCache);
                if (completedService != null)
                {
                    revenue += completedService.Price;
                }
            }

            popular.Add(new PopularServiceInsight
            {
                ServiceId = group.Key,
                ServiceName = service?.Name ?? "Unknown Service",
                Bookings = group.Count(),
                Revenue = revenue
            });
        }

        return popular
            .OrderByDescending(p => p.Bookings)
            .ThenByDescending(p => p.Revenue)
            .Take(5)
            .ToList();
    }

    private async Task<Service?> GetServiceAsync(string serviceId, IDictionary<string, Service?> cache)
    {
        if (cache.TryGetValue(serviceId, out var cached))
        {
            return cached;
        }

        var service = await _services.GetAsync(serviceId);
        cache[serviceId] = service;
        return service;
    }
}
