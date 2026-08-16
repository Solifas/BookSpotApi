using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.DTOs.Bookings;
using BookSpot.Application.Exceptions;
using BookSpot.Application.Features.Bookings.Commands;
using BookSpot.Domain.Entities;
using Moq;

namespace BookSpot.Tests;

public class BookingActionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BookingActionRequest_AcceptsOnlyActionVersionAndOptionalStart()
    {
        var properties = typeof(BookingActionRequest).GetProperties().Select(property => property.Name).ToArray();

        Assert.Equal(["Action", "ExpectedVersion", "StartTime"], properties);
    }

    [Fact]
    public async Task ApplyAction_ConcealsBookingFromNonPartyBeforeStateValidation()
    {
        var booking = Booking(status: "completed");
        var bookings = BookingRepository(booking);
        var businesses = new Mock<IBusinessRepository>();
        businesses.Setup(repository => repository.GetAsync(booking.BusinessId)).ReturnsAsync(new Business
        {
            Id = booking.BusinessId,
            ProviderId = "provider-1"
        });
        var claims = Claims("client-2", "client");
        var handler = Handler(bookings, businesses, claims);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new ApplyBookingActionCommand(booking.Id, "confirm", 1, null, "0123456789abcdef"),
            CancellationToken.None));

        bookings.Verify(repository => repository.ApplyActionAsync(It.IsAny<BookingActionPersistenceRequest>()), Times.Never);
    }

    [Fact]
    public async Task ApplyAction_OwningProviderCanConfirmPendingBooking()
    {
        var booking = Booking(status: "pending");
        var bookings = BookingRepository(booking);
        bookings.Setup(repository => repository.ApplyActionAsync(It.IsAny<BookingActionPersistenceRequest>()))
            .ReturnsAsync((BookingActionPersistenceRequest request) => request.Booking);
        var businesses = new Mock<IBusinessRepository>();
        businesses.Setup(repository => repository.GetAsync(booking.BusinessId)).ReturnsAsync(new Business
        {
            Id = booking.BusinessId,
            ProviderId = "provider-1"
        });
        var claims = Claims("provider-1", "provider");
        var handler = Handler(bookings, businesses, claims);

        var result = await handler.Handle(
            new ApplyBookingActionCommand(booking.Id, "confirm", 1, null, "0123456789abcdef"),
            CancellationToken.None);

        Assert.Equal("confirmed", result.Status);
        Assert.Equal(2, result.Version);
        bookings.Verify(repository => repository.ApplyActionAsync(It.Is<BookingActionPersistenceRequest>(request =>
            request.SourceStatus == "pending" &&
            request.SourceVersion == 1 &&
            request.Action == "confirm" &&
            request.ActorProfileId == "provider-1" &&
            request.ActorRole == "provider")));
    }

    [Fact]
    public async Task ApplyAction_RejectsStaleExpectedVersionWithoutWriting()
    {
        var booking = Booking(status: "pending", version: 2);
        var bookings = BookingRepository(booking);
        var claims = Claims("client-1", "client");
        var handler = Handler(bookings, new Mock<IBusinessRepository>(), claims);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new ApplyBookingActionCommand(booking.Id, "cancel", 1, null, "0123456789abcdef"),
            CancellationToken.None));

        bookings.Verify(repository => repository.ApplyActionAsync(It.IsAny<BookingActionPersistenceRequest>()), Times.Never);
    }

    [Fact]
    public async Task ApplyAction_RescheduleDerivesEndFromExistingDurationAndResetsConfirmation()
    {
        var booking = Booking(status: "confirmed");
        var bookings = BookingRepository(booking);
        bookings.Setup(repository => repository.ApplyActionAsync(It.IsAny<BookingActionPersistenceRequest>()))
            .ReturnsAsync((BookingActionPersistenceRequest request) => request.Booking);
        var services = new Mock<IServiceRepository>();
        services.Setup(repository => repository.GetAsync(booking.ServiceId)).ReturnsAsync(new Service
        {
            Id = booking.ServiceId,
            BusinessId = booking.BusinessId,
            IsActive = true
        });
        var businesses = new Mock<IBusinessRepository>();
        businesses.Setup(repository => repository.GetAsync(booking.BusinessId)).ReturnsAsync(new Business
        {
            Id = booking.BusinessId,
            ProviderId = "provider-1",
            IsActive = true
        });
        var claims = Claims("client-1", "client");
        var newStart = Now.AddDays(2);
        var handler = Handler(bookings, businesses, claims, services);

        var result = await handler.Handle(
            new ApplyBookingActionCommand(booking.Id, "reschedule", 1, newStart, "0123456789abcdef"),
            CancellationToken.None);

        Assert.Equal("pending", result.Status);
        Assert.Equal(newStart.UtcDateTime, result.StartTime);
        Assert.Equal(newStart.AddMinutes(30).UtcDateTime, result.EndTime);
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task ApplyAction_RejectsCancellationAtOrAfterStart()
    {
        var booking = Booking(status: "pending");
        booking.StartTime = Now.UtcDateTime;
        booking.EndTime = Now.AddMinutes(30).UtcDateTime;
        var bookings = BookingRepository(booking);
        var claims = Claims("client-1", "client");
        var handler = Handler(bookings, new Mock<IBusinessRepository>(), claims);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new ApplyBookingActionCommand(booking.Id, "cancel", 1, null, "0123456789abcdef"),
            CancellationToken.None));

        bookings.Verify(repository => repository.ApplyActionAsync(It.IsAny<BookingActionPersistenceRequest>()), Times.Never);
    }

    private static ApplyBookingActionHandler Handler(
        Mock<IBookingRepository> bookings,
        Mock<IBusinessRepository> businesses,
        Mock<IClaimsService> claims,
        Mock<IServiceRepository>? services = null) =>
        new(bookings.Object, businesses.Object, (services ?? new Mock<IServiceRepository>()).Object, claims.Object, new FixedTimeProvider(Now));

    private static Mock<IBookingRepository> BookingRepository(Booking booking)
    {
        var repository = new Mock<IBookingRepository>();
        repository.Setup(value => value.GetAsync(booking.Id)).ReturnsAsync(booking);
        return repository;
    }

    private static Mock<IClaimsService> Claims(string profileId, string role)
    {
        var claims = new Mock<IClaimsService>();
        claims.Setup(service => service.GetCurrentUserId()).Returns(profileId);
        claims.Setup(service => service.GetCurrentUserType()).Returns(role);
        return claims;
    }

    private static Booking Booking(string status, int version = 1) => new()
    {
        Id = "booking-1",
        ServiceId = "service-1",
        BusinessId = "business-1",
        ClientId = "client-1",
        ProviderProfileId = "provider-1",
        Status = status,
        StartTime = Now.AddDays(1).UtcDateTime,
        EndTime = Now.AddDays(1).AddMinutes(30).UtcDateTime,
        CreatedAt = Now.AddDays(-1).UtcDateTime,
        UpdatedAt = Now.AddDays(-1).UtcDateTime,
        Version = version
    };

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
