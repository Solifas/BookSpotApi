using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Reviews.Commands;

public record DeleteReviewCommand(string Id) : IRequest<bool>;

public class DeleteReviewHandler : IRequestHandler<DeleteReviewCommand, bool>
{
    private readonly IReviewRepository _reviews;
    public DeleteReviewHandler(IReviewRepository reviews) => _reviews = reviews;

    public async Task<bool> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var existing = await _reviews.GetAsync(request.Id);
        if (existing is null) return false;
        await _reviews.DeleteAsync(request.Id);
        return true;
    }
}