using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Services.Commands;

public record DeleteServiceCommand(string Id) : IRequest<bool>;

public class DeleteServiceHandler : IRequestHandler<DeleteServiceCommand, bool>
{
    private readonly IServiceRepository _services;
    public DeleteServiceHandler(IServiceRepository services) => _services = services;

    public async Task<bool> Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _services.GetAsync(request.Id);
        if (existing is null) return false;
        await _services.DeleteAsync(request.Id);
        return true;
    }
}