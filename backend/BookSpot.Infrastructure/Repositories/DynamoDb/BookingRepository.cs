using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Exceptions;
using BookSpot.Domain.Entities;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BookSpot.Infrastructure.Repositories.DynamoDb;

public class BookingRepository : IBookingRepository
{
    private readonly IDynamoDBContext _context;
    private readonly IAmazonDynamoDB _dynamoDb;

    public BookingRepository(IDynamoDBContext context, IAmazonDynamoDB dynamoDb)
    {
        _context = context;
        _dynamoDb = dynamoDb;
    }

    public async Task<Booking?> GetAsync(string id) => await _context.LoadAsync<Booking>(id);

    public async Task<Booking> CreateAsync(Booking booking, string idempotencyKey, string requestFingerprint)
    {
        var requestKey = $"REQ#v2#create#{Digest(idempotencyKey)}";
        var actorBinding = Digest(booking.ClientId);
        var fingerprint = Digest(requestFingerprint);
        var replay = await TryGetReplayAsync(requestKey, actorBinding, fingerprint);
        if (replay is not null) return replay;

        var transaction = new List<TransactWriteItem>
        {
            new()
            {
                Put = new Put
                {
                    TableName = "booking_reservations",
                    ConditionExpression = "attribute_not_exists(ReservationKey)",
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["ReservationKey"] = S(requestKey),
                        ["Kind"] = S("BOOKING_REQUEST"),
                        ["Operation"] = S("create"),
                        ["ActorBinding"] = S(actorBinding),
                        ["RequestFingerprint"] = S(fingerprint),
                        ["BookingId"] = S(booking.Id),
                        ["CommittedAtUtc"] = S(booking.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
                        ["ReplayExpiresAtUtc"] = S(booking.CreatedAt.AddHours(24).ToString("O", CultureInfo.InvariantCulture)),
                        ["SchemaVersion"] = N(2)
                    }
                }
            },
            new()
            {
                Put = new Put
                {
                    TableName = "bookings",
                    ConditionExpression = "attribute_not_exists(Id)",
                    Item = BookingItem(booking)
                }
            },
            new()
            {
                Put = new Put
                {
                    TableName = "booking_audit",
                    ConditionExpression = "attribute_not_exists(AuditKey)",
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["AuditKey"] = S($"AUD#v1#{Encode(booking.Id)}#0000000001"),
                        ["Kind"] = S("BOOKING_MUTATION"),
                        ["BookingId"] = S(booking.Id),
                        ["BusinessId"] = S(booking.BusinessId),
                        ["ServiceId"] = S(booking.ServiceId),
                        ["ActorProfileId"] = S(booking.ClientId),
                        ["ActorRole"] = S("client"),
                        ["Operation"] = S("create"),
                        ["ToStatus"] = S("pending"),
                        ["ToVersion"] = N(1),
                        ["OccurredAtUtc"] = S(booking.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
                        ["SchemaVersion"] = N(1)
                    }
                }
            }
        };

        for (var cell = booking.StartTime; cell < booking.EndTime; cell = cell.AddMinutes(15))
        {
            transaction.Add(new TransactWriteItem
            {
                Put = new Put
                {
                    TableName = "booking_reservations",
                    ConditionExpression = "attribute_not_exists(ReservationKey)",
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["ReservationKey"] = S(SlotKey(booking.BusinessId, cell)),
                        ["Kind"] = S("SLOT"),
                        ["BookingId"] = S(booking.Id),
                        ["BusinessId"] = S(booking.BusinessId),
                        ["ProviderProfileId"] = S(booking.ProviderProfileId),
                        ["ResourceId"] = S("single"),
                        ["StartTimeUtc"] = S(cell.ToString("O", CultureInfo.InvariantCulture)),
                        ["EndTimeUtc"] = S(cell.AddMinutes(15).ToString("O", CultureInfo.InvariantCulture)),
                        ["Status"] = S("pending"),
                        ["CreatedAtUtc"] = S(booking.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
                        ["SchemaVersion"] = N(1)
                    }
                }
            });
        }

        try
        {
            await _dynamoDb.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                ClientRequestToken = Digest(requestKey)[..32],
                TransactItems = transaction
            });
            return booking;
        }
        catch (TransactionCanceledException)
        {
            replay = await TryGetReplayAsync(requestKey, actorBinding, fingerprint);
            if (replay is not null) return replay;
            throw new ConflictException("The requested booking slot is no longer available.");
        }
    }

    public async Task<Booking> ApplyActionAsync(BookingActionPersistenceRequest request)
    {
        var booking = request.Booking;
        var requestKey = $"REQ#v2#{request.Action}#{Digest(request.IdempotencyKey)}";
        var actorBinding = Digest(request.ActorProfileId);
        var fingerprint = Digest(request.RequestFingerprint);
        var replay = await TryGetReplayAsync(requestKey, actorBinding, fingerprint);
        if (replay is not null) return replay;

        var transaction = new List<TransactWriteItem>
        {
            new()
            {
                Put = new Put
                {
                    TableName = "booking_reservations",
                    ConditionExpression = "attribute_not_exists(ReservationKey)",
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["ReservationKey"] = S(requestKey),
                        ["Kind"] = S("BOOKING_REQUEST"),
                        ["Operation"] = S(request.Action),
                        ["ActorBinding"] = S(actorBinding),
                        ["RequestFingerprint"] = S(fingerprint),
                        ["BookingId"] = S(booking.Id),
                        ["CommittedBookingVersion"] = N(booking.Version),
                        ["CommittedBookingStatus"] = S(booking.Status),
                        ["CommittedAtUtc"] = S(booking.UpdatedAt.ToString("O", CultureInfo.InvariantCulture)),
                        ["ReplayExpiresAtUtc"] = S(booking.UpdatedAt.AddHours(24).ToString("O", CultureInfo.InvariantCulture)),
                        ["HttpStatusCode"] = N(200),
                        ["SchemaVersion"] = N(2)
                    }
                }
            },
            new()
            {
                Update = new Update
                {
                    TableName = "bookings",
                    Key = new Dictionary<string, AttributeValue> { ["Id"] = S(booking.Id) },
                    ConditionExpression = "#status = :sourceStatus AND Version = :sourceVersion AND ClientId = :clientId AND BusinessId = :businessId AND ServiceId = :serviceId",
                    UpdateExpression = "SET #status = :targetStatus, StartTime = :startTime, EndTime = :endTime, UpdatedAt = :updatedAt, Version = :targetVersion",
                    ExpressionAttributeNames = new Dictionary<string, string> { ["#status"] = "Status" },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":sourceStatus"] = S(request.SourceStatus),
                        [":sourceVersion"] = N(request.SourceVersion),
                        [":clientId"] = S(booking.ClientId),
                        [":businessId"] = S(booking.BusinessId),
                        [":serviceId"] = S(booking.ServiceId),
                        [":targetStatus"] = S(booking.Status),
                        [":startTime"] = S(booking.StartTime.ToString("O", CultureInfo.InvariantCulture)),
                        [":endTime"] = S(booking.EndTime.ToString("O", CultureInfo.InvariantCulture)),
                        [":updatedAt"] = S(booking.UpdatedAt.ToString("O", CultureInfo.InvariantCulture)),
                        [":targetVersion"] = N(booking.Version)
                    }
                }
            },
            new()
            {
                Put = new Put
                {
                    TableName = "booking_audit",
                    ConditionExpression = "attribute_not_exists(AuditKey)",
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["AuditKey"] = S($"AUD#v1#{Encode(booking.Id)}#{booking.Version:D10}"),
                        ["Kind"] = S("BOOKING_MUTATION"),
                        ["BookingId"] = S(booking.Id),
                        ["BusinessId"] = S(booking.BusinessId),
                        ["ServiceId"] = S(booking.ServiceId),
                        ["ActorProfileId"] = S(request.ActorProfileId),
                        ["ActorRole"] = S(request.ActorRole),
                        ["Operation"] = S(request.Action),
                        ["FromStatus"] = S(request.SourceStatus),
                        ["ToStatus"] = S(booking.Status),
                        ["FromVersion"] = N(request.SourceVersion),
                        ["ToVersion"] = N(booking.Version),
                        ["OldStartTimeUtc"] = S(request.OldStartTime.ToString("O", CultureInfo.InvariantCulture)),
                        ["OldEndTimeUtc"] = S(request.OldEndTime.ToString("O", CultureInfo.InvariantCulture)),
                        ["NewStartTimeUtc"] = S(booking.StartTime.ToString("O", CultureInfo.InvariantCulture)),
                        ["NewEndTimeUtc"] = S(booking.EndTime.ToString("O", CultureInfo.InvariantCulture)),
                        ["OccurredAtUtc"] = S(booking.UpdatedAt.ToString("O", CultureInfo.InvariantCulture)),
                        ["SchemaVersion"] = N(1)
                    }
                }
            }
        };

        AddSlotMutations(transaction, request);

        try
        {
            await _dynamoDb.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                ClientRequestToken = Digest(requestKey)[..32],
                TransactItems = transaction
            });
            return booking;
        }
        catch (TransactionCanceledException)
        {
            replay = await TryGetReplayAsync(requestKey, actorBinding, fingerprint);
            if (replay is not null) return replay;
            throw new ConflictException("Booking state changed or the requested slot is no longer available.");
        }
    }

    public Task SaveAsync(Booking booking) => _context.SaveAsync(booking);
    public Task DeleteAsync(string id) => _context.DeleteAsync<Booking>(id);

    public async Task<IEnumerable<Booking>> GetConflictingBookingsAsync(string providerId, DateTime startTime, DateTime endTime)
    {
        var search = _context.ScanAsync<Booking>(new List<ScanCondition>
        {
            new("ProviderProfileId", ScanOperator.Equal, providerId)
        });
        var allBookings = await search.GetRemainingAsync();
        return allBookings.Where(booking =>
            booking.Status is "pending" or "confirmed" && startTime < booking.EndTime && endTime > booking.StartTime);
    }

    public async Task<IEnumerable<Booking>> GetBookingsByProviderAsync(string providerId)
    {
        var search = _context.ScanAsync<Booking>(new List<ScanCondition>
        {
            new("ProviderProfileId", ScanOperator.Equal, providerId)
        });
        return await search.GetRemainingAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByClientAsync(string clientId)
    {
        var search = _context.ScanAsync<Booking>(new List<ScanCondition>
        {
            new("ClientId", ScanOperator.Equal, clientId)
        });
        return await search.GetRemainingAsync();
    }

    private async Task<Booking?> TryGetReplayAsync(string requestKey, string actorBinding, string fingerprint)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = "booking_reservations",
            ConsistentRead = true,
            Key = new Dictionary<string, AttributeValue> { ["ReservationKey"] = S(requestKey) }
        });
        if (response.Item.Count == 0) return null;
        if (response.Item.GetValueOrDefault("ActorBinding")?.S != actorBinding ||
            response.Item.GetValueOrDefault("RequestFingerprint")?.S != fingerprint)
        {
            throw new ConflictException("The idempotency key was already used for a different request.");
        }

        var bookingId = response.Item["BookingId"].S;
        return await _context.LoadAsync<Booking>(bookingId, new DynamoDBOperationConfig { ConsistentRead = true });
    }

    private static void AddSlotMutations(List<TransactWriteItem> transaction, BookingActionPersistenceRequest request)
    {
        var oldCells = Cells(request.Booking.BusinessId, request.OldStartTime, request.OldEndTime);
        var newCells = Cells(request.Booking.BusinessId, request.Booking.StartTime, request.Booking.EndTime);

        if (request.Action == "confirm")
        {
            foreach (var key in oldCells.Keys) transaction.Add(UpdateSlotStatus(key, request.Booking.Id, "pending", "confirmed"));
            return;
        }

        if (request.Action != "reschedule")
        {
            foreach (var key in oldCells.Keys) transaction.Add(DeleteOwnedSlot(key, request.Booking.Id, request.SourceStatus));
            return;
        }

        foreach (var (key, cell) in newCells.Where(pair => !oldCells.ContainsKey(pair.Key)))
        {
            transaction.Add(new TransactWriteItem
            {
                Put = new Put
                {
                    TableName = "booking_reservations",
                    ConditionExpression = "attribute_not_exists(ReservationKey)",
                    Item = SlotItem(request.Booking, key, cell, "pending")
                }
            });
        }
        foreach (var key in oldCells.Keys.Where(key => !newCells.ContainsKey(key)))
        {
            transaction.Add(DeleteOwnedSlot(key, request.Booking.Id, request.SourceStatus));
        }
        if (request.SourceStatus == "confirmed")
        {
            foreach (var key in oldCells.Keys.Where(newCells.ContainsKey))
            {
                transaction.Add(UpdateSlotStatus(key, request.Booking.Id, "confirmed", "pending"));
            }
        }
    }

    private static Dictionary<string, DateTime> Cells(string businessId, DateTime start, DateTime end)
    {
        var cells = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        for (var cell = start; cell < end; cell = cell.AddMinutes(15)) cells[SlotKey(businessId, cell)] = cell;
        return cells;
    }

    private static TransactWriteItem DeleteOwnedSlot(string key, string bookingId, string status) => new()
    {
        Delete = new Delete
        {
            TableName = "booking_reservations",
            Key = new Dictionary<string, AttributeValue> { ["ReservationKey"] = S(key) },
            ConditionExpression = "BookingId = :bookingId AND #status = :status",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#status"] = "Status" },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":bookingId"] = S(bookingId),
                [":status"] = S(status)
            }
        }
    };

    private static TransactWriteItem UpdateSlotStatus(string key, string bookingId, string source, string target) => new()
    {
        Update = new Update
        {
            TableName = "booking_reservations",
            Key = new Dictionary<string, AttributeValue> { ["ReservationKey"] = S(key) },
            ConditionExpression = "BookingId = :bookingId AND #status = :sourceStatus",
            UpdateExpression = "SET #status = :targetStatus",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#status"] = "Status" },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":bookingId"] = S(bookingId),
                [":sourceStatus"] = S(source),
                [":targetStatus"] = S(target)
            }
        }
    };

    private static Dictionary<string, AttributeValue> SlotItem(Booking booking, string key, DateTime cell, string status) => new()
    {
        ["ReservationKey"] = S(key),
        ["Kind"] = S("SLOT"),
        ["BookingId"] = S(booking.Id),
        ["BusinessId"] = S(booking.BusinessId),
        ["ProviderProfileId"] = S(booking.ProviderProfileId),
        ["ResourceId"] = S("single"),
        ["StartTimeUtc"] = S(cell.ToString("O", CultureInfo.InvariantCulture)),
        ["EndTimeUtc"] = S(cell.AddMinutes(15).ToString("O", CultureInfo.InvariantCulture)),
        ["Status"] = S(status),
        ["CreatedAtUtc"] = S(booking.UpdatedAt.ToString("O", CultureInfo.InvariantCulture)),
        ["SchemaVersion"] = N(1)
    };

    private static Dictionary<string, AttributeValue> BookingItem(Booking booking) => new()
    {
        ["Id"] = S(booking.Id),
        ["ServiceId"] = S(booking.ServiceId),
        ["BusinessId"] = S(booking.BusinessId),
        ["ClientId"] = S(booking.ClientId),
        ["ProviderId"] = S(booking.ProviderId),
        ["ProviderProfileId"] = S(booking.ProviderProfileId),
        ["ProviderName"] = S(booking.ProviderName ?? string.Empty),
        ["StartTime"] = S(booking.StartTime.ToString("O", CultureInfo.InvariantCulture)),
        ["EndTime"] = S(booking.EndTime.ToString("O", CultureInfo.InvariantCulture)),
        ["Status"] = S(booking.Status),
        ["CreatedAt"] = S(booking.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
        ["UpdatedAt"] = S(booking.UpdatedAt.ToString("O", CultureInfo.InvariantCulture)),
        ["Version"] = N(booking.Version)
    };

    private static string SlotKey(string businessId, DateTime startTime) =>
        $"SLOT#v1#{Encode(businessId)}#{Encode("single")}#{startTime.ToUniversalTime():yyyyMMdd'T'HHmmss'Z'}";

    private static string Digest(string value) => Encode(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string Encode(string value) => Encode(Encoding.UTF8.GetBytes(value));
    private static string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static AttributeValue S(string value) => new() { S = value };
    private static AttributeValue N(int value) => new() { N = value.ToString(CultureInfo.InvariantCulture) };
}
