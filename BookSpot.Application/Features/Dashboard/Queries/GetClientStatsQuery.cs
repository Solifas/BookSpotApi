using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.DTOs.Dashboard;
using BookSpot.Application.Exceptions;
using MediatR;

namespace BookSpot.Application.Features.Dashboard.Queries;

public record GetClientStatsQuery(string ClientId) : IRequest<ClientStatsDto>;

public class GetClientStatsHandler : IRequestHandler<GetClientStatsQuery, ClientStatsDto>
{
    private readonly IProfileRepository _profiles;
    private readonly IBookingRepository _bookings;
    private readonly IServiceRepository _services;

    public GetClientStatsHandler(
        IProfileRepository profiles,
        IBookingRepository bookings,
        IServiceRepository services)
    {
        _profiles = profiles;
        _bookings = bookings;
        _services = services;
    }

    public async Task<ClientStatsDto> Handle(GetClientStatsQuery request, CancellationToken cancellationToken)
    {
        // Verify client exists
        var client = await _profiles.GetAsync(request.ClientId);
        if (client == null)
        {
            throw new NotFoundException($"Client with ID '{request.ClientId}' not found.");
        }

        if (client.UserType != "client")
        {
            throw new ValidationException($"User with ID '{request.ClientId}' is not a client.");
        }

        // For now, return mock data so frontend can use it immediately
        // TODO: Replace with actual database queries
        var mockStats = new ClientStatsDto
        {
            ClientId = request.ClientId,
            FullName = client.FullName,
            Email = client.Email,
            ContactNumber = client.ContactNumber,
            TotalBookings = 15,
            CompletedBookings = 12,
            CancelledBookings = 2,
            TotalSpent = 1250.00m,
            FirstVisit = DateTime.UtcNow.AddMonths(-8),
            LastVisit = DateTime.UtcNow.AddDays(-3),
            FavoriteService = "Hair Cut & Style",
            RecentBookings = new List<RecentBookingDto>
            {
                new RecentBookingDto
                {
                    Id = "booking-001",
                    ServiceName = "Hair Cut & Style",
                    StartTime = DateTime.UtcNow.AddDays(-3),
                    Status = "completed",
                    Price = 85.00m
                },
                new RecentBookingDto
                {
                    Id = "booking-002",
                    ServiceName = "Manicure",
                    StartTime = DateTime.UtcNow.AddDays(-14),
                    Status = "completed",
                    Price = 45.00m
                },
                new RecentBookingDto
                {
                    Id = "booking-003",
                    ServiceName = "Facial Treatment",
                    StartTime = DateTime.UtcNow.AddDays(-28),
                    Status = "completed",
                    Price = 120.00m
                },
                new RecentBookingDto
                {
                    Id = "booking-004",
                    ServiceName = "Hair Color",
                    StartTime = DateTime.UtcNow.AddDays(-42),
                    Status = "cancelled",
                    Price = 150.00m
                },
                new RecentBookingDto
                {
                    Id = "booking-005",
                    ServiceName = "Massage Therapy",
                    StartTime = DateTime.UtcNow.AddDays(-56),
                    Status = "completed",
                    Price = 95.00m
                }
            }
        };

        return mockStats;
    }
}