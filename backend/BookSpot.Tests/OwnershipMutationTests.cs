using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using BookSpot.Application.Features.Businesses.Commands;
using BookSpot.Application.Features.BusinessHours.Commands;
using BookSpot.Application.Features.Reviews.Commands;
using BookSpot.Application.Features.Services.Commands;
using BookSpot.Domain.Entities;
using Moq;

namespace BookSpot.Tests;

public class OwnershipMutationTests
{
    [Fact]
    public async Task DeleteBusiness_CrossAccountIsConcealedAndDoesNotDelete()
    {
        var businesses = new Mock<IBusinessRepository>();
        businesses.Setup(repository => repository.GetAsync("business-1"))
            .ReturnsAsync(new Business { Id = "business-1", ProviderId = "provider-1" });
        var handler = new DeleteBusinessHandler(businesses.Object, Claims("provider-2", "provider").Object);

        var deleted = await handler.Handle(new DeleteBusinessCommand("business-1"), CancellationToken.None);

        Assert.False(deleted);
        businesses.Verify(repository => repository.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteService_UsesBusinessOwnershipNotDenormalizedServiceProviderId()
    {
        var services = new Mock<IServiceRepository>();
        services.Setup(repository => repository.GetAsync("service-1")).ReturnsAsync(new Service
        {
            Id = "service-1",
            BusinessId = "business-1",
            ProviderId = "provider-2"
        });
        var businesses = new Mock<IBusinessRepository>();
        businesses.Setup(repository => repository.GetAsync("business-1"))
            .ReturnsAsync(new Business { Id = "business-1", ProviderId = "provider-1" });
        var handler = new DeleteServiceHandler(services.Object, businesses.Object, Claims("provider-1", "provider").Object);

        var deleted = await handler.Handle(new DeleteServiceCommand("service-1"), CancellationToken.None);

        Assert.True(deleted);
        services.Verify(repository => repository.DeleteAsync("service-1"), Times.Once);
    }

    [Fact]
    public async Task UpdateBusinessHour_CrossAccountIsConcealedAndDoesNotWrite()
    {
        var hours = new Mock<IBusinessHourRepository>();
        hours.Setup(repository => repository.GetAsync("hour-1"))
            .ReturnsAsync(new BusinessHour { Id = "hour-1", BusinessId = "business-1" });
        var businesses = new Mock<IBusinessRepository>();
        businesses.Setup(repository => repository.GetAsync("business-1"))
            .ReturnsAsync(new Business { Id = "business-1", ProviderId = "provider-1" });
        var handler = new UpdateBusinessHourHandler(hours.Object, businesses.Object, Claims("provider-2", "provider").Object);

        var result = await handler.Handle(
            new UpdateBusinessHourCommand("hour-1", 1, "09:00", "17:00", false),
            CancellationToken.None);

        Assert.Null(result);
        hours.Verify(repository => repository.SaveAsync(It.IsAny<BusinessHour>()), Times.Never);
    }

    [Fact]
    public async Task CreateReview_RequiresCompletedBookingOwnedByClient()
    {
        var reviews = new Mock<IReviewRepository>();
        var bookings = new Mock<IBookingRepository>();
        bookings.Setup(repository => repository.GetAsync("booking-1")).ReturnsAsync(new Booking
        {
            Id = "booking-1",
            ClientId = "client-2",
            Status = "completed"
        });
        var handler = new CreateReviewHandler(reviews.Object, bookings.Object, Claims("client-1", "client").Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new CreateReviewCommand("booking-1", 5, "Great"), CancellationToken.None));

        reviews.Verify(repository => repository.SaveAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task DeleteReview_OnlyBookingClientCanDelete()
    {
        var reviews = new Mock<IReviewRepository>();
        reviews.Setup(repository => repository.GetAsync("review-1"))
            .ReturnsAsync(new Review { Id = "review-1", BookingId = "booking-1" });
        var bookings = new Mock<IBookingRepository>();
        bookings.Setup(repository => repository.GetAsync("booking-1")).ReturnsAsync(new Booking
        {
            Id = "booking-1",
            ClientId = "client-2",
            Status = "completed"
        });
        var handler = new DeleteReviewHandler(reviews.Object, bookings.Object, Claims("client-1", "client").Object);

        var deleted = await handler.Handle(new DeleteReviewCommand("review-1"), CancellationToken.None);

        Assert.False(deleted);
        reviews.Verify(repository => repository.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    private static Mock<IClaimsService> Claims(string profileId, string role)
    {
        var claims = new Mock<IClaimsService>();
        claims.Setup(service => service.GetCurrentUserId()).Returns(profileId);
        claims.Setup(service => service.GetCurrentUserType()).Returns(role);
        claims.Setup(service => service.IsProvider()).Returns(role == "provider");
        claims.Setup(service => service.IsClient()).Returns(role == "client");
        return claims;
    }
}
