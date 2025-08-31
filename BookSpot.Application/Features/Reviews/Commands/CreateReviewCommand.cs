using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Reviews.Commands;

public record CreateReviewCommand(string BookingId, int Rating, string Comment) : IRequest<Review>;

public class CreateReviewHandler : IRequestHandler<CreateReviewCommand, Review>
{
    private readonly IReviewRepository _reviews;
    public CreateReviewHandler(IReviewRepository reviews) => _reviews = reviews;

    public async Task<Review> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = new Review
        {
            Id = Guid.NewGuid().ToString(),
            BookingId = request.BookingId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        await _reviews.SaveAsync(review);
        return review;
    }
}