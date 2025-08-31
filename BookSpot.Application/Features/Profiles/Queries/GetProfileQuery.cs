using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Profiles.Queries;

public record GetProfileQuery(string Id) : IRequest<Profile?>;

public class GetProfileHandler : IRequestHandler<GetProfileQuery, Profile?>
{
    private readonly IProfileRepository _profiles;
    public GetProfileHandler(IProfileRepository profiles) => _profiles = profiles;

    public Task<Profile?> Handle(GetProfileQuery request, CancellationToken cancellationToken)
        => _profiles.GetAsync(request.Id);
}