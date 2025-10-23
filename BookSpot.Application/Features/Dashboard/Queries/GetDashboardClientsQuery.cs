using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.DTOs.Dashboard;
using BookSpot.Application.Exceptions;
using MediatR;

namespace BookSpot.Application.Features.Dashboard.Queries;

public record GetDashboardClientsQuery(string ProviderId) : IRequest<IEnumerable<DashboardClientDto>>;

public class GetDashboardClientsHandler : IRequestHandler<GetDashboardClientsQuery, IEnumerable<DashboardClientDto>>
{
    private readonly IProfileRepository _profiles;
    private readonly IBookingRepository _bookings;

    public GetDashboardClientsHandler(
        IProfileRepository profiles,
        IBookingRepository bookings)
    {
        _profiles = profiles;
        _bookings = bookings;
    }

    public async Task<IEnumerable<DashboardClientDto>> Handle(GetDashboardClientsQuery request, CancellationToken cancellationToken)
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

        // For now, return mock data so frontend can use it immediately
        // TODO: Replace with actual database queries
        var mockClients = new List<DashboardClientDto>
        {
            new DashboardClientDto
            {
                Id = "client-001",
                FullName = "Sarah Johnson",
                Email = "sarah.johnson@email.com",
                ContactNumber = "+1-555-0123",
                TotalBookings = 12,
                LastVisit = DateTime.UtcNow.AddDays(-3)
            },
            new DashboardClientDto
            {
                Id = "client-002",
                FullName = "Michael Chen",
                Email = "michael.chen@email.com",
                ContactNumber = "+1-555-0456",
                TotalBookings = 8,
                LastVisit = DateTime.UtcNow.AddDays(-7)
            },
            new DashboardClientDto
            {
                Id = "client-003",
                FullName = "Emma Rodriguez",
                Email = "emma.rodriguez@email.com",
                ContactNumber = null,
                TotalBookings = 15,
                LastVisit = DateTime.UtcNow.AddDays(-1)
            },
            new DashboardClientDto
            {
                Id = "client-004",
                FullName = "David Thompson",
                Email = "david.thompson@email.com",
                ContactNumber = "+1-555-0789",
                TotalBookings = 5,
                LastVisit = DateTime.UtcNow.AddDays(-14)
            },
            new DashboardClientDto
            {
                Id = "client-005",
                FullName = "Lisa Park",
                Email = "lisa.park@email.com",
                ContactNumber = "+1-555-0321",
                TotalBookings = 20,
                LastVisit = DateTime.UtcNow.AddHours(-6)
            }
        };

        return mockClients.OrderByDescending(c => c.LastVisit);
    }
}