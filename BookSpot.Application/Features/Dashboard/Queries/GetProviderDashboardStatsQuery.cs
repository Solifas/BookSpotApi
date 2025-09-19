using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.DTOs.Dashboard;
using BookSpot.Application.Exceptions;
using MediatR;

namespace BookSpot.Application.Features.Dashboard.Queries;

public record GetProviderDashboardStatsQuery(string ProviderId) : IRequest<DashboardStats>;

public class GetProviderDashboardStatsHandler : IRequestHandler<GetProviderDashboardStatsQuery, DashboardStats>
{
    private readonly IBookingRepository _bookings;
    private readonly IServiceRepository _services;
    private readonly IProfileRepository _profiles;

    public GetProviderDashboardStatsHandler(
        IBookingRepository bookings,
        IServiceRepository services,
        IProfileRepository profiles)
    {
        _bookings = bookings;
        _services = services;
        _profiles = profiles;
    }

    public async Task<DashboardStats> Handle(GetProviderDashboardStatsQuery request, CancellationToken cancellationToken)
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

        // Get all bookings for this provider
        var allBookings = await _bookings.GetBookingsByProviderAsync(request.ProviderId);
        var bookingsList = allBookings.ToList();

        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        // Calculate stats
        var todayBookings = bookingsList.Count(b => b.StartTime.Date == today);
        var weekBookings = bookingsList.Count(b => b.StartTime.Date >= weekStart);
        var totalClients = bookingsList.Select(b => b.ClientId).Distinct().Count();
        var pendingBookings = bookingsList.Count(b => b.Status.Equals("pending", StringComparison.OrdinalIgnoreCase));
        var confirmedBookings = bookingsList.Count(b => b.Status.Equals("confirmed", StringComparison.OrdinalIgnoreCase));
        var completedBookings = bookingsList.Count(b => b.Status.Equals("completed", StringComparison.OrdinalIgnoreCase));
        var cancelledBookings = bookingsList.Count(b => b.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase));

        // Calculate monthly revenue from completed bookings
        var monthlyCompletedBookings = bookingsList.Where(b =>
            b.StartTime >= monthStart &&
            b.Status.Equals("completed", StringComparison.OrdinalIgnoreCase));

        decimal monthlyRevenue = 0;
        foreach (var booking in monthlyCompletedBookings)
        {
            var service = await _services.GetAsync(booking.ServiceId);
            if (service != null)
            {
                monthlyRevenue += service.Price;
            }
        }

        // Calculate average booking value
        decimal totalRevenue = 0;
        var revenueBookings = bookingsList.Where(b =>
            b.Status.Equals("completed", StringComparison.OrdinalIgnoreCase));

        foreach (var booking in revenueBookings)
        {
            var service = await _services.GetAsync(booking.ServiceId);
            if (service != null)
            {
                totalRevenue += service.Price;
            }
        }

        var averageBookingValue = completedBookings > 0 ? totalRevenue / completedBookings : 0;

        return new DashboardStats
        {
            TodayBookings = todayBookings,
            WeekBookings = weekBookings,
            TotalClients = totalClients,
            MonthlyRevenue = monthlyRevenue,
            PendingBookings = pendingBookings,
            ConfirmedBookings = confirmedBookings,
            CompletedBookings = completedBookings,
            CancelledBookings = cancelledBookings,
            AverageBookingValue = averageBookingValue,
            TotalBookings = bookingsList.Count,
            StatsGeneratedAt = DateTime.UtcNow
        };
    }
}