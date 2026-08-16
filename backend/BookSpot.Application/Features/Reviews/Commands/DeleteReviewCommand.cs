using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using MediatR;

namespace BookSpot.Application.Features.Reviews.Commands;

public record DeleteReviewCommand(string Id) : IRequest<bool>;

public class DeleteReviewHandler : IRequestHandler<DeleteReviewCommand, bool>
{
    private readonly IReviewRepository _reviews;
    private readonly IBookingRepository _bookings;
    private readonly IClaimsService _claims;
    public DeleteReviewHandler(IReviewRepository reviews, IBookingRepository bookings, IClaimsService claims)
    {
        _reviews = reviews;
        _bookings = bookings;
        _claims = claims;
    }

    public async Task<bool> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var existing = await _reviews.GetAsync(request.Id);
        if (existing is null) return false;
        var booking = await _bookings.GetAsync(existing.BookingId);
        if (booking is null || !_claims.IsClient() ||
            !string.Equals(booking.ClientId, _claims.GetCurrentUserId(), StringComparison.Ordinal)) return false;
        await _reviews.DeleteAsync(request.Id);
        return true;
    }
}