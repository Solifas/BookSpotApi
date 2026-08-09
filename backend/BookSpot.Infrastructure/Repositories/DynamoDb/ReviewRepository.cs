using Amazon.DynamoDBv2.DataModel;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Domain.Entities;

namespace BookSpot.Infrastructure.Repositories.DynamoDb;

public class ReviewRepository : IReviewRepository
{
    private readonly IDynamoDBContext _context;
    public ReviewRepository(IDynamoDBContext context) => _context = context;

    public async Task<Review?> GetAsync(string id) => await _context.LoadAsync<Review>(id);
    public Task SaveAsync(Review review) => _context.SaveAsync(review);
    public Task DeleteAsync(string id) => _context.DeleteAsync<Review>(id);
}