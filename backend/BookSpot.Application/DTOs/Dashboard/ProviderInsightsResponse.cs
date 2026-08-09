using System.Collections.Generic;

namespace BookSpot.Application.DTOs.Dashboard;

public class ProviderInsightsResponse
{
    public ProviderInsightsStats Stats { get; set; } = new();
    public List<PopularServiceInsight> PopularServices { get; set; } = [];
}

public class ProviderInsightsStats
{
    public int TodayBookings { get; set; }
    public int WeekBookings { get; set; }
    public int TotalClients { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
}

public class PopularServiceInsight
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public int Bookings { get; set; }
    public decimal Revenue { get; set; }
}
