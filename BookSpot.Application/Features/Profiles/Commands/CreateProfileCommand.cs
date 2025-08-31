using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Profiles.Commands;

public record CreateProfileCommand(string Email, string UserType) : IRequest<Profile>;

public class CreateProfileHandler : IRequestHandler<CreateProfileCommand, Profile>
{
    private readonly IProfileRepository _profiles;
    public CreateProfileHandler(IProfileRepository profiles) => _profiles = profiles;

    public async Task<Profile> Handle(CreateProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = new Profile
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserType = request.UserType,
            CreatedAt = DateTime.UtcNow
        };

        await _profiles.SaveAsync(profile);
        return profile;
    }
}