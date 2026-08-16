using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Features.Bookings.Queries;
using BookSpot.Domain.Entities;
using Moq;

namespace BookSpot.Tests;

public class BookingAuthorizationTests
{
    [Fact]
    public async Task GetBooking_ConcealsBookingFromNonParty()
    {
        var booking = new Booking
        {
            Id = "booking-1",
            ClientId = "client-1",
            BusinessId = "business-1",
            ServiceId = "service-1",
            ProviderProfileId = "provider-1"
        };
        var bookings = new Mock<IBookingRepository>();
        bookings.Setup(repository => repository.GetAsync("booking-1")).ReturnsAsync(booking);
        var businesses = new Mock<IBusinessRepository>();
        businesses.Setup(repository => repository.GetAsync("business-1")).ReturnsAsync(new Business
        {
            Id = "business-1",
            ProviderId = "provider-1"
        });
        var claims = new Mock<IClaimsService>();
        claims.Setup(service => service.GetCurrentUserId()).Returns("client-2");
        var handler = new GetBookingHandler(bookings.Object, businesses.Object, claims.Object);

        var result = await handler.Handle(new GetBookingQuery("booking-1"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBooking_ReturnsBookingToClientOwner()
    {
        var booking = new Booking { Id = "booking-1", ClientId = "client-1", BusinessId = "business-1" };
        var bookings = new Mock<IBookingRepository>();
        bookings.Setup(repository => repository.GetAsync("booking-1")).ReturnsAsync(booking);
        var businesses = new Mock<IBusinessRepository>();
        var claims = new Mock<IClaimsService>();
        claims.Setup(service => service.GetCurrentUserId()).Returns("client-1");
        var handler = new GetBookingHandler(bookings.Object, businesses.Object, claims.Object);

        var result = await handler.Handle(new GetBookingQuery("booking-1"), CancellationToken.None);

        Assert.Same(booking, result);
        businesses.Verify(repository => repository.GetAsync(It.IsAny<string>()), Times.Never);
    }
}
