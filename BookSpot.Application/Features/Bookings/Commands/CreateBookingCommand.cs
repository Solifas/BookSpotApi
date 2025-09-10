using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using MediatR;

namespace BookSpot.Application.Features.Bookings.Commands;

public record CreateBookingCommand(string ServiceId, DateTime StartTime, DateTime EndTime) : IRequest<Booking>;

public class CreateBookingHandler : IRequestHandler<CreateBookingCommand, Booking>
{
    private readonly IBookingRepository _bookings;
    private readonly IServiceRepository _services;
    private readonly IProfileRepository _profiles;
    private readonly IBusinessRepository _businesses;
    private readonly IClaimsService _claimsService;

    public CreateBookingHandler(
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

    public async Task<Booking> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        // Get current user from JWT claims
        var currentUserId = _claimsService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new ValidationException("User must be authenticated to create a booking.");
        }

        // Validate that the current user exists and is a client
        var currentUser = await _profiles.GetAsync(currentUserId);
        if (currentUser == null)
        {
            throw new NotFoundException("Current user profile not found.");
        }

        if (currentUser.UserType != "client")
        {
            throw new ValidationException("Only clients can create bookings.");
        }

        // Validate that the service exists
        var service = await _services.GetAsync(request.ServiceId);
        if (service == null)
        {
            throw new NotFoundException($"Service with ID '{request.ServiceId}' not found.");
        }

        // Validate that the business associated with the service exists and is active
        var business = await _businesses.GetAsync(service.BusinessId);
        if (business == null)
        {
            throw new NotFoundException($"Business associated with service '{request.ServiceId}' not found.");
        }

        if (!business.IsActive)
        {
            throw new ValidationException($"Business associated with service '{request.ServiceId}' is not active.");
        }

        // Get the provider from the business
        var provider = await _profiles.GetAsync(business.ProviderId);
        if (provider == null)
        {
            throw new NotFoundException($"Provider profile not found for business.");
        }

        // Calculate end time based on service duration
        var endTime = request.StartTime.AddMinutes(service.DurationMinutes);

        // Validate booking times
        if (request.StartTime >= endTime)
        {
            throw new ValidationException("Invalid booking time calculation.");
        }

        if (request.StartTime <= DateTime.UtcNow)
        {
            throw new ValidationException("Booking start time must be in the future.");
        }

        // Validate that the provided end time matches the calculated end time (if provided)
        if (request.EndTime != default && Math.Abs((request.EndTime - endTime).TotalMinutes) > 1)
        {
            throw new ValidationException($"End time must match service duration. Expected: {endTime:yyyy-MM-dd HH:mm}, Provided: {request.EndTime:yyyy-MM-dd HH:mm}");
        }

        // Use calculated end time
        var finalEndTime = request.EndTime != default ? request.EndTime : endTime;

        // Check for booking conflicts
        var conflictingBookings = await _bookings.GetConflictingBookingsAsync(business.ProviderId, request.StartTime, finalEndTime);
        if (conflictingBookings.Any())
        {
            throw new ValidationException($"Provider already has a booking during the requested time slot.");
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid().ToString(),
            ServiceId = request.ServiceId,
            ClientId = currentUserId,
            ProviderId = business.ProviderId,
            StartTime = request.StartTime,
            EndTime = finalEndTime,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await _bookings.SaveAsync(booking);
        return booking;
    }
}