using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Reviews.Queries;

public record GetReviewQuery(string Id) : IRequest<Review?>;

public class GetReviewHandler : IRequestHandler<GetReviewQuery, Review?>
{
    private readonly IReviewRepository _reviews;
    public GetReviewHandler(IReviewRepository reviews) => _reviews = reviews;

    public Task<Review?> Handle(GetReviewQuery request, CancellationToken cancellationToken)
        => _reviews.GetAsync(request.Id);
}