using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.DTOs.Bookings;
using BookSpot.Application.Exceptions;
using MediatR;

namespace BookSpot.Application.Features.Bookings.Queries;

public record GetClientBookingsQuery(
    string ClientId,
    string? Status = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<IEnumerable<BookingWithDetails>>;

public class GetClientBookingsHandler : IRequestHandler<GetClientBookingsQuery, IEnumerable<BookingWithDetails>>
{
    private readonly IBookingRepository _bookings;
    private readonly IServiceRepository _services;
    private readonly IProfileRepository _profiles;
    private readonly IBusinessRepository _businesses;
    private readonly IClaimsService _claimsService;

    public GetClientBookingsHandler(
        IBookingRepository bookings,
        IServiceRepository services,
        IProfileRepository profiles,
        IBusinessRepository businesses,
        IClaimsService claimsService)
    {
        _bookings = bookings;
        _services = services;
        _profiles = profiles;
        _businesses = businesses;
        _claimsService = claimsService;
    }

    public async Task<IEnumerable<BookingWithDetails>> Handle(GetClientBookingsQuery request, CancellationToken cancellationToken)
    {
        // Get current user ID from claims
        var currentUserId = _claimsService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new ValidationException("User not authenticated.");
        }

        // Verify that the authenticated user is requesting their own bookings
        if (currentUserId != request.ClientId)
        {
            throw new ValidationException("Access denied. You can only view your own bookings.");
        }

        // Verify client exists and is actually a client
        var client = await _profiles.GetAsync(request.ClientId);
        if (client == null)
        {
            throw new NotFoundException($"Client with ID '{request.ClientId}' not found.");
        }

        if (client.UserType != "client")
        {
            throw new ValidationException($"User with ID '{request.ClientId}' is not a client.");
        }

        // Get all bookings for this client
        var clientBookings = await _bookings.GetBookingsByClientAsync(request.ClientId);

        // Apply filters
        var filteredBookings = clientBookings.AsEnumerable();

        if (!string.IsNullOrEmpty(request.Status))
        {
            filteredBookings = filteredBookings.Where(b =>
                b.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (request.StartDate.HasValue)
        {
            filteredBookings = filteredBookings.Where(b => b.StartTime >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            filteredBookings = filteredBookings.Where(b => b.StartTime <= request.EndDate.Value);
        }

        // Convert to BookingWithDetails
        var bookingsWithDetails = new List<BookingWithDetails>();

        foreach (var booking in filteredBookings)
        {
            var service = await _services.GetAsync(booking.ServiceId);
            var provider = await _profiles.GetAsync(service.BusinessId);

            if (service is not null && provider is not null)
            {
                bookingsWithDetails.Add(new BookingWithDetails
                {
                    Id = booking.Id,
                    ServiceId = booking.ServiceId,
                    ClientId = booking.ClientId,
                    ProviderId = booking.ProviderId,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    Status = booking.Status,
                    CreatedAt = booking.CreatedAt,
                    Service = new ServiceDetails
                    {
                        Id = service.Id,
                        Name = service.Name,
                        Description = service.Description,
                        Category = service.Category,
                        Price = service.Price,
                        DurationMinutes = service.DurationMinutes,
                        ImageUrl = service.ImageUrl,
                        Tags = service.Tags
                    },
                    Client = new ClientDetails
                    {
                        Id = client.Id,
                        FullName = client.FullName,
                        Email = client.Email,
                        ContactNumber = client.ContactNumber
                    }
                });
            }
        }

        return bookingsWithDetails.OrderByDescending(b => b.StartTime);
    }
}