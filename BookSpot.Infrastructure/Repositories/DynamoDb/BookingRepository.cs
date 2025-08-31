using Amazon.DynamoDBv2.DataModel;
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
}