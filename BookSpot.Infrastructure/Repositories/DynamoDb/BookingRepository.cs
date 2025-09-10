using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Domain.Entities;

namespace BookSpot.Infrastructure.Repositories.DynamoDb;

public class BookingRepository : IBookingRepository
{
    private readonly IDynamoDBContext _context;
    public BookingRepository(IDynamoDBContext context) => _context = context;

    public async Task<Booking?> GetAsync(string id) => await _context.LoadAsync<Booking>(id);
    public Task SaveAsync(Booking booking) => _context.SaveAsync(booking);
    public Task DeleteAsync(string id) => _context.DeleteAsync<Booking>(id);

    public async Task<IEnumerable<Booking>> GetConflictingBookingsAsync(string providerId, DateTime startTime, DateTime endTime)
    {
        var scanConditions = new List<ScanCondition>
        {
            new("ProviderId", ScanOperator.Equal, providerId),
            new("Status", ScanOperator.NotEqual, "cancelled")
        };

        var search = _context.ScanAsync<Booking>(scanConditions);
        var allBookings = await search.GetRemainingAsync();

        // Filter for time conflicts in memory since DynamoDB doesn't support complex date range queries easily
        return allBookings.Where(booking => 
            (startTime < booking.EndTime && endTime > booking.StartTime));
    }
}