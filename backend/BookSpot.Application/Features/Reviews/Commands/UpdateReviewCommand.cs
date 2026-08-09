using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Reviews.Commands;

public record UpdateReviewCommand(string Id, int Rating, string Comment) : IRequest<Review?>;

public class UpdateReviewHandler : IRequestHandler<UpdateReviewCommand, Review?>
{
    private readonly IReviewRepository _reviews;
    public UpdateReviewHandler(IReviewRepository reviews) => _reviews = reviews;

    public async Task<Review?> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var existing = await _reviews.GetAsync(request.Id);
        if (existing is null) return null;

        existing.Rating = request.Rating;
        existing.Comment = request.Comment;

        await _reviews.SaveAsync(existing);
        return existing;
    }
}