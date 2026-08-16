using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Profiles.Commands;

public record UpdateProfileCommand(string Id, string? FullName, string? ContactNumber) : IRequest<Profile?>;

public class UpdateProfileHandler(IProfileRepository profiles) : IRequestHandler<UpdateProfileCommand, Profile?>
{
    public async Task<Profile?> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var existing = await profiles.GetAsync(request.Id);
        if (existing is null) return null;

        if (request.FullName is not null) existing.FullName = request.FullName.Trim();
        existing.ContactNumber = request.ContactNumber;
        await profiles.SaveAsync(existing);
        return existing;
    }
}
