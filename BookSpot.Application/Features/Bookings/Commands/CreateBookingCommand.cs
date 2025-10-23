using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;
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

        // Validate that the service exists
        var service = await _services.GetAsync(request.ServiceId);
        if (service == null)
        {
            throw new NotFoundException($"Service with ID '{request.ServiceId}' not found.");
        }

        if (service.BusinessId == currentUserId)
        {
            throw new BadRequestException("You cannot book your own service");
        }

        if (!service.IsActive)
        {
            throw new ValidationException($"Business associated with service '{request.ServiceId}' is not active.");
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
        // if (request.EndTime != default && Math.Abs((request.EndTime - endTime).TotalMinutes) > 1)
        // {
        //     throw new ValidationException($"End time must match service duration. Expected: {endTime:yyyy-MM-dd HH:mm}, Provided: {request.EndTime:yyyy-MM-dd HH:mm}");
        // }

        // Use calculated end time
        var finalEndTime = request.EndTime != default ? request.EndTime : endTime;

        // Check for booking conflicts
        var conflictingBookings = await _bookings.GetConflictingBookingsAsync(service.BusinessId, request.StartTime, finalEndTime);
        if (conflictingBookings.Any())
        {
            throw new ValidationException($"Provider already has a booking during the requested time slot.");
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid().ToString(),
            ServiceId = request.ServiceId,
            ClientId = currentUserId,
            ProviderId = service.BusinessId,
            StartTime = request.StartTime,
            EndTime = finalEndTime,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await _bookings.SaveAsync(booking);
        return booking;
    }
}