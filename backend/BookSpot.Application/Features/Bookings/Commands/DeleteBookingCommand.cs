using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Bookings.Commands;

public record DeleteBookingCommand(string Id) : IRequest<bool>;

public class DeleteBookingHandler : IRequestHandler<DeleteBookingCommand, bool>
{
    private readonly IBookingRepository _bookings;
    public DeleteBookingHandler(IBookingRepository bookings) => _bookings = bookings;

    public async Task<bool> Handle(DeleteBookingCommand request, CancellationToken cancellationToken)
    {
        var existing = await _bookings.GetAsync(request.Id);
        if (existing is null) return false;
        await _bookings.DeleteAsync(request.Id);
        return true;
    }
}