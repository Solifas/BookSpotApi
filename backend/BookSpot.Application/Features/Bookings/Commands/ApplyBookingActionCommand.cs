using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;
using MediatR;

namespace BookSpot.Application.Features.Bookings.Commands;

public sealed record ApplyBookingActionCommand(
    string BookingId,
    string Action,
    int ExpectedVersion,
    DateTimeOffset? StartTime,
    string IdempotencyKey) : IRequest<Booking>;

public sealed class ApplyBookingActionHandler : IRequestHandler<ApplyBookingActionCommand, Booking>
{
    private readonly IBookingRepository _bookings;
    private readonly IBusinessRepository _businesses;
    private readonly IServiceRepository _services;
    private readonly IClaimsService _claims;
    private readonly TimeProvider _timeProvider;

    public ApplyBookingActionHandler(
        IBookingRepository bookings,
        IBusinessRepository businesses,
        IServiceRepository services,
        IClaimsService claims,
        TimeProvider timeProvider)
    {
        _bookings = bookings;
        _businesses = businesses;
        _services = services;
        _claims = claims;
        _timeProvider = timeProvider;
    }

    public async Task<Booking> Handle(ApplyBookingActionCommand request, CancellationToken cancellationToken)
    {
        var booking = await _bookings.GetAsync(request.BookingId);
        if (booking is null) throw new NotFoundException("Booking not found.");

        var actorProfileId = _claims.GetCurrentUserId();
        var actorRole = _claims.GetCurrentUserType();
        if (string.IsNullOrWhiteSpace(actorProfileId) || actorRole is not ("client" or "provider"))
        {
            throw new NotFoundException("Booking not found.");
        }

        if (actorRole == "client")
        {
            if (!string.Equals(booking.ClientId, actorProfileId, StringComparison.Ordinal))
            {
                throw new NotFoundException("Booking not found.");
            }
        }
        else
        {
            var business = await _businesses.GetAsync(booking.BusinessId);
            if (business is null || !string.Equals(business.ProviderId, actorProfileId, StringComparison.Ordinal))
            {
                throw new NotFoundException("Booking not found.");
            }
        }

        if (request.ExpectedVersion <= 0) throw new BadRequestException("Expected version must be positive.");
        if (booking.Version != request.ExpectedVersion) throw new ConflictException("Booking state changed.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length is < 16 or > 128)
        {
            throw new BadRequestException("A valid Idempotency-Key is required.");
        }
        if (request.Action == "reschedule" && request.StartTime is null)
        {
            throw new BadRequestException("Start time is required for reschedule.");
        }
        if (request.Action != "reschedule" && request.StartTime is not null)
        {
            throw new BadRequestException("Start time is only valid for reschedule.");
        }
        if (!BookingLifecycle.TryTransition(booking.Status, request.Action, actorRole, out var targetStatus))
        {
            throw new ConflictException("Booking state changed.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (request.Action == "cancel" && now >= booking.StartTime)
        {
            throw new ConflictException("The booking can no longer be cancelled.");
        }
        if (request.Action == "complete" && now < booking.EndTime)
        {
            throw new ConflictException("The booking cannot be completed before it ends.");
        }
        if (request.Action == "mark_no_show" && now < booking.StartTime)
        {
            throw new ConflictException("The booking cannot be marked no-show before it starts.");
        }

        var sourceStatus = booking.Status;
        var sourceVersion = booking.Version;
        var oldStartTime = booking.StartTime;
        var oldEndTime = booking.EndTime;

        if (request.Action == "reschedule")
        {
            var service = await _services.GetAsync(booking.ServiceId);
            var business = await _businesses.GetAsync(booking.BusinessId);
            if (service is null || business is null || !service.IsActive || !business.IsActive ||
                !string.Equals(service.BusinessId, booking.BusinessId, StringComparison.Ordinal))
            {
                throw new NotFoundException("Booking target not found.");
            }

            var startTime = request.StartTime!.Value.UtcDateTime;
            if (startTime <= now) throw new BadRequestException("Booking start time must be in the future.");
            var duration = oldEndTime - oldStartTime;
            if (duration <= TimeSpan.Zero) throw new ConflictException("Booking duration is invalid.");
            booking.StartTime = startTime;
            booking.EndTime = startTime.Add(duration);
        }

        booking.Status = targetStatus;
        booking.Version = sourceVersion + 1;
        booking.UpdatedAt = now;

        var fingerprint = string.Join('\n', actorProfileId, request.Action, booking.Id,
            sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            request.Action == "reschedule" ? booking.StartTime.ToString("O") : string.Empty);

        return await _bookings.ApplyActionAsync(new BookingActionPersistenceRequest(
            booking,
            request.Action,
            sourceStatus,
            sourceVersion,
            oldStartTime,
            oldEndTime,
            actorProfileId,
            actorRole,
            request.IdempotencyKey,
            fingerprint));
    }
}
