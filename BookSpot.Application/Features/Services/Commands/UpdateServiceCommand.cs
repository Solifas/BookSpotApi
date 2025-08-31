using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Services.Commands;

public record UpdateServiceCommand(string Id, string Name, decimal Price, int DurationMinutes) : IRequest<Service?>;

public class UpdateServiceHandler : IRequestHandler<UpdateServiceCommand, Service?>
{
    private readonly IServiceRepository _services;
    public UpdateServiceHandler(IServiceRepository services) => _services = services;

    public async Task<Service?> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _services.GetAsync(request.Id);
        if (existing is null) return null;

        existing.Name = request.Name;
        existing.Price = request.Price;
        existing.DurationMinutes = request.DurationMinutes;

        await _services.SaveAsync(existing);
        return existing;
    }
}