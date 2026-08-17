using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using MediatR;
using System.Security.Cryptography;
using System.Text;

namespace BookSpot.Application.Features.Reviews.Commands;

public record CreateReviewCommand(string BookingId, int Rating, string Comment) : IRequest<Review>;

public class CreateReviewHandler : IRequestHandler<CreateReviewCommand, Review>
{
    private readonly IReviewRepository _reviews;
    private readonly IBookingRepository _bookings;
    private readonly IClaimsService _claims;
    public CreateReviewHandler(IReviewRepository reviews, IBookingRepository bookings, IClaimsService claims)
    {
        _reviews = reviews;
        _bookings = bookings;
        _claims = claims;
    }

    public async Task<Review> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var booking = await _bookings.GetAsync(request.BookingId);
        if (booking is null || !_claims.IsClient() ||
            !string.Equals(booking.ClientId, _claims.GetCurrentUserId(), StringComparison.Ordinal))
        {
            throw new NotFoundException("Booking not found.");
        }
        if (booking.Status != "completed") throw new ConflictException("Only completed bookings can be reviewed.");
        if (await _reviews.GetByBookingAsync(request.BookingId) is not null)
            throw new ConflictException("The booking already has a review.");

        var review = new Review
        {
            Id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.BookingId))).ToLowerInvariant(),
            BookingId = request.BookingId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        if (!await _reviews.CreateAsync(review))
            throw new ConflictException("The booking already has a review.");
        return review;
    }
}