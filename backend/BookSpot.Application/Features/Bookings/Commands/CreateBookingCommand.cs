using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using BookSpot.Application.Features.Availability;
using BookSpot.Domain.Entities;
using MediatR;

namespace BookSpot.Application.Features.Bookings.Commands;

public record CreateBookingCommand(string ServiceId, DateTimeOffset StartTime, string IdempotencyKey) : IRequest<Booking>;

public class CreateBookingHandler : IRequestHandler<CreateBookingCommand, Booking>
{
    private readonly IBookingRepository _bookings;
    private readonly IServiceRepository _services;
    private readonly IProfileRepository _profiles;
    private readonly IBusinessRepository _businesses;
    private readonly IBusinessHourRepository _businessHours;
    private readonly IClaimsService _claimsService;

    public CreateBookingHandler(
        IBookingRepository bookings,
        IServiceRepository services,
        IProfileRepository profiles,
        IBusinessRepository businesses,
        IBusinessHourRepository businessHours,
        IClaimsService claimsService)
    {
        _bookings = bookings;
        _services = services;
        _profiles = profiles;
        _businesses = businesses;
        _businessHours = businessHours;
        _claimsService = claimsService;
    }

    public async Task<Booking> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        // Get current user from JWT claims
        var currentUserId = _claimsService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId) || !_claimsService.IsClient())
        {
            throw new ValidationException("User must be authenticated to create a booking.");
        }

        // Validate that the service exists
        var service = await _services.GetAsync(request.ServiceId);
        if (service == null)
        {
            throw new NotFoundException($"Service with ID '{request.ServiceId}' not found.");
        }

        if (!service.IsActive)
        {
            throw new ValidationException($"Business associated with service '{request.ServiceId}' is not active.");
        }

        var business = await _businesses.GetAsync(service.BusinessId);
        if (business is null || !business.IsActive)
        {
            throw new NotFoundException("Service not found.");
        }

        var startTime = request.StartTime.UtcDateTime;
        var endTime = startTime.AddMinutes(service.DurationMinutes);

        // Validate booking times
        if (startTime >= endTime)
        {
            throw new ValidationException("Invalid booking time calculation.");
        }

        if (startTime <= DateTime.UtcNow)
        {
            throw new ValidationException("Booking start time must be in the future.");
        }

        var schedule = await _businessHours.GetByBusinessAsync(business.Id);
        var zoneName = string.IsNullOrWhiteSpace(business.TimeZone)
            ? Application.DTOs.Canonical.CanonicalDtoMapper.DefaultTimeZone
            : business.TimeZone;
        var offered = AvailabilityCalculator.Calculate(service, schedule, Array.Empty<Booking>(),
            request.StartTime, request.StartTime.AddMinutes(service.DurationMinutes), zoneName);
        if (offered.Slots.All(slot => slot.StartTime != request.StartTime.ToUniversalTime()))
            throw new ConflictException("The requested booking slot is outside business hours.",
                "booking_slot_conflict");

        var clientProfile = await _profiles.GetAsync(currentUserId) ??
            throw new NotFoundException("Profile", currentUserId);
        var now = DateTime.UtcNow;
        var booking = new Booking
        {
            Id = Guid.NewGuid().ToString(),
            ServiceId = request.ServiceId,
            BusinessId = business.Id,
            ClientId = currentUserId,
            ProviderId = business.ProviderId,
            ProviderProfileId = business.ProviderId,
            StartTime = startTime,
            EndTime = endTime,
            Status = "pending",
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1,
            ProviderName = service.ProviderName,
            PriceAmount = service.Price,
            ServiceNameSnapshot = service.Name,
            DurationMinutesSnapshot = service.DurationMinutes,
            BusinessNameSnapshot = business.BusinessName,
            BusinessAddressSnapshot = business.Address,
            BusinessCitySnapshot = business.City,
            ClientFullNameSnapshot = clientProfile.FullName,
            ClientEmailSnapshot = clientProfile.Email,
            ClientPhoneSnapshot = clientProfile.ContactNumber
        };

        var fingerprint = $"{currentUserId}\n{request.ServiceId}\n{startTime:O}";
        return await _bookings.CreateAsync(booking, request.IdempotencyKey, fingerprint);
    }
}
