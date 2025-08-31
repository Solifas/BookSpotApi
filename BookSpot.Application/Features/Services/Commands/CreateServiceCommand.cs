using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Services.Commands;

public record CreateServiceCommand(string BusinessId, string Name, decimal Price, int DurationMinutes) : IRequest<Service>;

public class CreateServiceHandler : IRequestHandler<CreateServiceCommand, Service>
{
    private readonly IServiceRepository _services;
    public CreateServiceHandler(IServiceRepository services) => _services = services;

    public async Task<Service> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = new Service
        {
            Id = Guid.NewGuid().ToString(),
            BusinessId = request.BusinessId,
            Name = request.Name,
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            CreatedAt = DateTime.UtcNow
        };

        await _services.SaveAsync(service);
        return service;
    }
}