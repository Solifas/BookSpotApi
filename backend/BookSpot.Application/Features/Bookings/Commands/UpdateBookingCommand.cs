using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Bookings.Commands;

public record UpdateBookingCommand(string Id, DateTime StartTime, DateTime EndTime, string Status) : IRequest<Booking?>;

public class UpdateBookingHandler : IRequestHandler<UpdateBookingCommand, Booking?>
{
    private readonly IBookingRepository _bookings;
    public UpdateBookingHandler(IBookingRepository bookings) => _bookings = bookings;

    public async Task<Booking?> Handle(UpdateBookingCommand request, CancellationToken cancellationToken)
    {
        var existing = await _bookings.GetAsync(request.Id);
        if (existing is null) return null;

        existing.StartTime = request.StartTime;
        existing.EndTime = request.EndTime;
        existing.Status = request.Status;

        await _bookings.SaveAsync(existing);
        return existing;
    }
}