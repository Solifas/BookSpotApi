using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Profiles.Commands;

public record UpdateProfileCommand(string Id, string Email, string UserType) : IRequest<Profile?>;

public class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, Profile?>
{
    private readonly IProfileRepository _profiles;
    public UpdateProfileHandler(IProfileRepository profiles) => _profiles = profiles;

    public async Task<Profile?> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var existing = await _profiles.GetAsync(request.Id);
        if (existing is null) return null;

        existing.Email = request.Email;
        existing.UserType = request.UserType;

        await _profiles.SaveAsync(existing);
        return existing;
    }
}