namespace BookSpot.Application.DTOs.Dashboard;

public class DashboardStats
{
    public int TodayBookings { get; set; }
    public int WeekBookings { get; set; }
    public int TotalClients { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }

    // Additional useful stats
    public decimal AverageBookingValue { get; set; }
    public int TotalBookings { get; set; }
    public DateTime StatsGeneratedAt { get; set; } = DateTime.UtcNow;
}