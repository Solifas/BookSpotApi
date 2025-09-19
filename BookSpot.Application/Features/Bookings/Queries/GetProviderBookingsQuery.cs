using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.DTOs.Bookings;
using BookSpot.Application.Exceptions;
using MediatR;

namespace BookSpot.Application.Features.Bookings.Queries;

public record GetProviderBookingsQuery(
    string ProviderId,
    string? Status = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<IEnumerable<BookingWithDetails>>;

public class GetProviderBookingsHandler : IRequestHandler<GetProviderBookingsQuery, IEnumerable<BookingWithDetails>>
{
    private readonly IBookingRepository _bookings;
    private readonly IServiceRepository _services;
    private readonly IProfileRepository _profiles;
    private readonly IBusinessRepository _businesses;

    public GetProviderBookingsHandler(
        IBookingRepository bookings,
        IServiceRepository services,
        IProfileRepository profiles,
        IBusinessRepository businesses)
    {
        _bookings = bookings;
        _services = services;
        _profiles = profiles;
        _businesses = businesses;
    }

    public async Task<IEnumerable<BookingWithDetails>> Handle(GetProviderBookingsQuery request, CancellationToken cancellationToken)
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

        // Apply filters
        var filteredBookings = allBookings.AsEnumerable();

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
            var client = await _profiles.GetAsync(booking.ClientId);
            var business = service != null ? await _businesses.GetAsync(service.BusinessId) : null;

            if (service != null && client != null && business != null)
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
                    },
                    Business = new BusinessDetails
                    {
                        Id = business.Id,
                        BusinessName = business.BusinessName,
                        Description = business.Description,
                        Address = business.Address,
                        Phone = business.Phone,
                        Email = business.Email,
                        City = business.City,
                        Website = business.Website,
                        ImageUrl = business.ImageUrl
                    }
                });
            }
        }

        return bookingsWithDetails.OrderByDescending(b => b.StartTime);
    }
}