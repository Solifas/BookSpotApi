using System;

namespace BookSpot.Application.DTOs.Dashboard;

public class DashboardClientDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public int TotalBookings { get; set; }
    public DateTime? LastVisit { get; set; }
}