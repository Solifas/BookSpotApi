using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Bookings.Queries;

public record GetBookingQuery(string Id) : IRequest<Booking?>;

public class GetBookingHandler : IRequestHandler<GetBookingQuery, Booking?>
{
    private readonly IBookingRepository _bookings;
    public GetBookingHandler(IBookingRepository bookings) => _bookings = bookings;

    public Task<Booking?> Handle(GetBookingQuery request, CancellationToken cancellationToken)
        => _bookings.GetAsync(request.Id);
}