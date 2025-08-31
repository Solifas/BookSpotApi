using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Profiles.Commands;

public record DeleteProfileCommand(string Id) : IRequest<bool>;

public class DeleteProfileHandler : IRequestHandler<DeleteProfileCommand, bool>
{
    private readonly IProfileRepository _profiles;
    public DeleteProfileHandler(IProfileRepository profiles) => _profiles = profiles;

    public async Task<bool> Handle(DeleteProfileCommand request, CancellationToken cancellationToken)
    {
        var existing = await _profiles.GetAsync(request.Id);
        if (existing is null) return false;
        await _profiles.DeleteAsync(request.Id);
        return true;
    }
}