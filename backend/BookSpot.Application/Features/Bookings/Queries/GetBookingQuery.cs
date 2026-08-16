using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using MediatR;

namespace BookSpot.Application.Features.Bookings.Queries;

public record GetBookingQuery(string Id) : IRequest<Booking?>;

public class GetBookingHandler(
    IBookingRepository bookings,
    IBusinessRepository businesses,
    IClaimsService claims) : IRequestHandler<GetBookingQuery, Booking?>
{
    public async Task<Booking?> Handle(GetBookingQuery request, CancellationToken cancellationToken)
    {
        var subject = claims.GetCurrentUserId();
        if (subject is null) return null;

        var booking = await bookings.GetAsync(request.Id);
        if (booking is null) return null;
        if (string.Equals(booking.ClientId, subject, StringComparison.Ordinal)) return booking;

        var business = await businesses.GetAsync(booking.BusinessId);
        return business is not null && string.Equals(business.ProviderId, subject, StringComparison.Ordinal)
            ? booking
            : null;
    }
}