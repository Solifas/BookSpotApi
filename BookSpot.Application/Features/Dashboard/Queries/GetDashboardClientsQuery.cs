using System;
using System.Collections.Generic;
using System.Linq;

using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.DTOs.Dashboard;
using BookSpot.Application.Exceptions;
using MediatR;

namespace BookSpot.Application.Features.Dashboard.Queries;

public record GetDashboardClientsQuery(string ProviderId) : IRequest<IReadOnlyList<DashboardClientDto>>;

public class GetDashboardClientsHandler : IRequestHandler<GetDashboardClientsQuery, IReadOnlyList<DashboardClientDto>>
{
    private readonly IBookingRepository _bookings;
    private readonly IProfileRepository _profiles;

    public GetDashboardClientsHandler(IBookingRepository bookings, IProfileRepository profiles)
    {
        _bookings = bookings;
        _profiles = profiles;
    }

    public async Task<IReadOnlyList<DashboardClientDto>> Handle(
        GetDashboardClientsQuery request,
        CancellationToken cancellationToken)
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
        var bookingsByClient = bookings
            .Where(b => !string.IsNullOrWhiteSpace(b.ClientId))
            .GroupBy(b => b.ClientId!)
            .ToList();

        if (bookingsByClient.Count == 0)
        {
            return Array.Empty<DashboardClientDto>();
        }

        var clients = new List<DashboardClientDto>();

        foreach (var group in bookingsByClient)
        {
            var client = await _profiles.GetAsync(group.Key);
            if (client is null)
            {
                continue;
            }

            var lastVisit = group.Max(b => b.StartTime);

            clients.Add(new DashboardClientDto
            {
                Id = client.Id,
                FullName = client.FullName,
                Email = client.Email,
                ContactNumber = client.ContactNumber,
                TotalBookings = group.Count(),
                LastVisit = lastVisit
            });
        }

        return clients
            .OrderByDescending(c => c.LastVisit ?? DateTime.MinValue)
            .ThenBy(c => c.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}