using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Businesses.Commands;

public record DeleteBusinessCommand(string Id) : IRequest<bool>;

public class DeleteBusinessHandler : IRequestHandler<DeleteBusinessCommand, bool>
{
    private readonly IBusinessRepository _businesses;
    public DeleteBusinessHandler(IBusinessRepository businesses) => _businesses = businesses;

    public async Task<bool> Handle(DeleteBusinessCommand request, CancellationToken cancellationToken)
    {
        var existing = await _businesses.GetAsync(request.Id);
        if (existing is null) return false;
        await _businesses.DeleteAsync(request.Id);
        return true;
    }
}