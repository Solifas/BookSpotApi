using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Exceptions;
using BookSpot.Application.Features.Canonical.Queries;
using BookSpot.Domain.Entities;
using Moq;

namespace BookSpot.Tests;

public sealed class CanonicalQueryTests
{
    [Fact]
    public async Task ProviderBookingList_ConcealsUnownedBusinessFilter()
    {
        var bookings = new Mock<IBookingRepository>();
        var services = new Mock<IServiceRepository>();
        var businesses = new Mock<IBusinessRepository>();
        var profiles = new Mock<IProfileRepository>();
        businesses.Setup(repository => repository.GetAllAsync()).ReturnsAsync(new[]
        {
            new Business { Id = "owned-business", ProviderId = "provider-1" },
            new Business { Id = "other-business", ProviderId = "provider-2" }
        });
        var handler = new GetCanonicalBookingPageHandler(bookings.Object, services.Object, businesses.Object,
            profiles.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new GetCanonicalBookingPageQuery("provider-1", "provider", BusinessId: "other-business"),
            CancellationToken.None));

        bookings.Verify(repository => repository.GetBookingsByBusinessAsync(It.IsAny<string>()), Times.Never);
        bookings.Verify(repository => repository.GetBookingsByProviderAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ClientBookingList_ProjectsSnapshotsWhenRelatedRecordsWereDeleted()
    {
        var booking = new Booking
        {
            Id = "booking-1", ServiceId = "deleted-service", BusinessId = "deleted-business",
            ClientId = "client-1", ProviderProfileId = "provider-1", Status = "completed",
            StartTime = DateTime.UnixEpoch, EndTime = DateTime.UnixEpoch.AddMinutes(30),
            CreatedAt = DateTime.UnixEpoch, UpdatedAt = DateTime.UnixEpoch, Version = 1, PriceAmount = 100m,
            ServiceNameSnapshot = "Historical Service", DurationMinutesSnapshot = 30,
            BusinessNameSnapshot = "Historical Business", BusinessAddressSnapshot = "Historical Address",
            BusinessCitySnapshot = "Cape Town"
        };
        var bookings = new Mock<IBookingRepository>();
        bookings.Setup(value => value.GetBookingsByClientAsync("client-1")).ReturnsAsync([booking]);
        var handler = new GetCanonicalBookingPageHandler(bookings.Object, Mock.Of<IServiceRepository>(),
            Mock.Of<IBusinessRepository>(), Mock.Of<IProfileRepository>());

        var result = await handler.Handle(new GetCanonicalBookingPageQuery("client-1", "client"),
            CancellationToken.None);

        var projected = Assert.IsType<BookSpot.Application.DTOs.Canonical.ClientBookingDto>(Assert.Single(result.Items));
        Assert.Equal("Historical Service", projected.Service.Name);
        Assert.Equal("Historical Business", projected.Business.BusinessName);
    }
}
