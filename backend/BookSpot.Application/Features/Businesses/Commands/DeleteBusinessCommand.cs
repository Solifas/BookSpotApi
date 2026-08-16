using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using MediatR;

namespace BookSpot.Application.Features.Businesses.Commands;

public record DeleteBusinessCommand(string Id) : IRequest<bool>;

public class DeleteBusinessHandler : IRequestHandler<DeleteBusinessCommand, bool>
{
    private readonly IBusinessRepository _businesses;
    private readonly IClaimsService _claims;
    public DeleteBusinessHandler(IBusinessRepository businesses, IClaimsService claims)
    {
        _businesses = businesses;
        _claims = claims;
    }

    public async Task<bool> Handle(DeleteBusinessCommand request, CancellationToken cancellationToken)
    {
        var existing = await _businesses.GetAsync(request.Id);
        var actor = _claims.GetCurrentUserId();
        if (existing is null || !_claims.IsProvider() || !string.Equals(existing.ProviderId, actor, StringComparison.Ordinal)) return false;
        await _businesses.DeleteAsync(request.Id);
        return true;
    }
}