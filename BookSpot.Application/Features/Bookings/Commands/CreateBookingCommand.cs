using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Bookings.Commands;

public record CreateBookingCommand(string ServiceId, string ClientId, string ProviderId, DateTime StartTime, DateTime EndTime) : IRequest<Booking>;

public class CreateBookingHandler : IRequestHandler<CreateBookingCommand, Booking>
{
    private readonly IBookingRepository _bookings;
    public CreateBookingHandler(IBookingRepository bookings) => _bookings = bookings;

    public async Task<Booking> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid().ToString(),
            ServiceId = request.ServiceId,
            ClientId = request.ClientId,
            ProviderId = request.ProviderId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await _bookings.SaveAsync(booking);
        return booking;
    }
}