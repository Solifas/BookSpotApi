using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using BookSpot.Application.Features.Canonical.Queries;
using BookSpot.Application.Features.Availability;
using BookSpot.Domain.Entities;
using BookSpot.Infrastructure.Repositories.DynamoDb;
using Microsoft.Extensions.Configuration;
using Moq;

namespace BookSpot.Tests;

public sealed class LocalStackCanonicalIntegrationTests
{
    [Fact]
    public async Task ReviewCreate_IsAtomicForOneReviewPerBooking()
    {
        using var client = LocalStackClient();
        var context = new DynamoDBContext(client, new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 });
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(value => value["DynamoDB:Tables:Reviews"]).Returns("reviews");
        var repository = new ReviewRepository(context, client, configuration.Object);
        var reviewId = $"review-atomic-{Guid.NewGuid():N}";
        var review = new Review
        {
            Id = reviewId, BookingId = $"booking-{Guid.NewGuid():N}", Rating = 5,
            Comment = "Atomic", CreatedAt = DateTime.UtcNow
        };

        try
        {
            var results = await Task.WhenAll(repository.CreateAsync(review), repository.CreateAsync(review));
            Assert.Single(results, value => value);
        }
        finally
        {
            await context.DeleteAsync<Review>(reviewId);
            context.Dispose();
        }
    }

    [Fact]
    public async Task Availability_UsesPersistedBusinessScheduleAndBusinessBookings()
    {
        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = "http://localhost:4566",
            AuthenticationRegion = "us-east-1",
            UseHttp = true
        };
        using var dynamo = new AmazonDynamoDBClient("test", "test", config);
        var context = new DynamoDBContext(dynamo);
        var suffix = Guid.NewGuid().ToString("N");
        var business = new Business
        {
            Id = $"business-{suffix}", ProviderId = $"provider-{suffix}", BusinessName = "Integration Studio",
            IsActive = true, TimeZone = "Africa/Johannesburg", CreatedAt = DateTime.UtcNow
        };
        var service = new Service
        {
            Id = $"service-{suffix}", BusinessId = business.Id, Name = "Integration Service",
            DurationMinutes = 30, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        var hour = new BusinessHour
        {
            Id = $"hour-{suffix}", BusinessId = business.Id, DayOfWeek = (int)DayOfWeek.Monday,
            OpenTime = "09:00", CloseTime = "10:00", IsClosed = false
        };
        var booking = new Booking
        {
            Id = $"booking-{suffix}", BusinessId = business.Id, ServiceId = service.Id,
            ClientId = $"client-{suffix}", ProviderProfileId = "stale-denormalized-provider",
            StartTime = new DateTime(2026, 8, 17, 7, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 8, 17, 7, 30, 0, DateTimeKind.Utc),
            Status = "pending", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await context.SaveAsync(business);
            await context.SaveAsync(service);
            await context.SaveAsync(hour);
            await context.SaveAsync(booking);
            var bookingRepository = new BookingRepository(context, dynamo);
            var persistedBookings = (await bookingRepository.GetBookingsByBusinessAsync(business.Id)).ToArray();
            Assert.Single(persistedBookings);
            Assert.Equal("pending", persistedBookings[0].Status);
            Assert.Equal(booking.StartTime, persistedBookings[0].StartTime);
            Assert.Equal(booking.EndTime, persistedBookings[0].EndTime);
            var directResult = AvailabilityCalculator.Calculate(service, new[] { hour }, persistedBookings,
                new DateTimeOffset(2026, 8, 17, 7, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 17, 8, 0, 0, TimeSpan.Zero));
            Assert.DoesNotContain(directResult.Slots, slot =>
                slot.StartTime == new DateTimeOffset(2026, 8, 17, 7, 0, 0, TimeSpan.Zero));
            var handler = new GetServiceAvailabilityHandler(
                new ServiceRepository(context), new BusinessRepository(context),
                new BusinessHourRepository(context), bookingRepository);

            var result = await handler.Handle(new GetServiceAvailabilityQuery(service.Id,
                new DateTimeOffset(2026, 8, 17, 7, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 17, 8, 0, 0, TimeSpan.Zero)), CancellationToken.None);

            Assert.DoesNotContain(result.Slots, slot =>
                slot.StartTime == new DateTimeOffset(2026, 8, 17, 7, 0, 0, TimeSpan.Zero));
            Assert.Single(result.Slots);
            Assert.Equal(new DateTimeOffset(2026, 8, 17, 7, 30, 0, TimeSpan.Zero), result.Slots[0].StartTime);
        }
        finally
        {
            await context.DeleteAsync<Booking>(booking.Id);
            await context.DeleteAsync<BusinessHour>(hour.Id);
            await context.DeleteAsync<Service>(service.Id);
            await context.DeleteAsync<Business>(business.Id);
        }
    }

    private static AmazonDynamoDBClient LocalStackClient() => new("test", "test", new AmazonDynamoDBConfig
    {
        ServiceURL = "http://localhost:4566",
        AuthenticationRegion = "us-east-1",
        UseHttp = true
    });
}