using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
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
            ProviderName = service.ProviderName
        };

        var fingerprint = $"{currentUserId}\n{request.ServiceId}\n{startTime:O}";
        return await _bookings.CreateAsync(booking, request.IdempotencyKey, fingerprint);
    }
}
