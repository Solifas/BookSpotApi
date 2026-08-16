using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Domain.Entities;
using BookSpot.Infrastructure.Repositories.DynamoDb;
using Moq;

namespace BookSpot.Tests;

public class BookingRepositoryTransactionTests
{
    [Fact]
    public async Task ApplyAction_ConfirmCommitsRequestBookingAuditAndAllSlotUpdatesTogether()
    {
        var context = new Mock<IDynamoDBContext>();
        var dynamo = new Mock<IAmazonDynamoDB>();
        dynamo.Setup(client => client.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse { Item = [] });
        TransactWriteItemsRequest? captured = null;
        dynamo.Setup(client => client.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TransactWriteItemsRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new TransactWriteItemsResponse());
        var repository = new BookingRepository(context.Object, dynamo.Object);
        var start = new DateTime(2026, 8, 17, 10, 0, 0, DateTimeKind.Utc);
        var booking = Booking(start, "confirmed", 2);

        await repository.ApplyActionAsync(new BookingActionPersistenceRequest(
            booking, "confirm", "pending", 1, start, start.AddMinutes(30),
            "provider-1", "provider", "0123456789abcdef", "fingerprint"));

        Assert.NotNull(captured);
        Assert.Equal(5, captured.TransactItems.Count);
        Assert.Single(captured.TransactItems.Where(item => item.Put?.TableName == "booking_reservations"));
        Assert.Single(captured.TransactItems.Where(item => item.Update?.TableName == "bookings"));
        Assert.Single(captured.TransactItems.Where(item => item.Put?.TableName == "booking_audit"));
        Assert.Equal(2, captured.TransactItems.Count(item =>
            item.Update?.TableName == "booking_reservations" &&
            item.Update.UpdateExpression == "SET #status = :targetStatus"));
        Assert.All(captured.TransactItems.Where(item => item.Update?.TableName == "booking_reservations"), item =>
            Assert.Contains("BookingId = :bookingId", item.Update.ConditionExpression));
    }

    [Fact]
    public async Task ApplyAction_DisjointRescheduleAcquiresNewSlotsAndReleasesOldSlotsInOneTransaction()
    {
        var context = new Mock<IDynamoDBContext>();
        var dynamo = new Mock<IAmazonDynamoDB>();
        dynamo.Setup(client => client.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse { Item = [] });
        TransactWriteItemsRequest? captured = null;
        dynamo.Setup(client => client.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TransactWriteItemsRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new TransactWriteItemsResponse());
        var repository = new BookingRepository(context.Object, dynamo.Object);
        var oldStart = new DateTime(2026, 8, 17, 10, 0, 0, DateTimeKind.Utc);
        var newStart = oldStart.AddHours(2);
        var booking = Booking(newStart, "pending", 2);

        await repository.ApplyActionAsync(new BookingActionPersistenceRequest(
            booking, "reschedule", "confirmed", 1, oldStart, oldStart.AddMinutes(30),
            "client-1", "client", "0123456789abcdef", "fingerprint"));

        Assert.NotNull(captured);
        Assert.Equal(7, captured.TransactItems.Count);
        Assert.Equal(2, captured.TransactItems.Count(item =>
            item.Put?.TableName == "booking_reservations" && item.Put.Item["Kind"].S == "SLOT"));
        Assert.Equal(2, captured.TransactItems.Count(item => item.Delete?.TableName == "booking_reservations"));
        Assert.All(captured.TransactItems.Where(item => item.Delete is not null), item =>
            Assert.Contains("BookingId = :bookingId", item.Delete.ConditionExpression));
    }

    private static Booking Booking(DateTime start, string status, int version) => new()
    {
        Id = "booking-1",
        ServiceId = "service-1",
        BusinessId = "business-1",
        ClientId = "client-1",
        ProviderProfileId = "provider-1",
        Status = status,
        StartTime = start,
        EndTime = start.AddMinutes(30),
        CreatedAt = start.AddDays(-1),
        UpdatedAt = start.AddMinutes(-5),
        Version = version
    };
}
