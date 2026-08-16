using BookSpot.Application.DTOs.Bookings;
using BookSpot.Domain.Entities;

namespace BookSpot.Tests;

public class BookingSecurityContractTests
{
    [Fact]
    public void CreateBookingRequest_AcceptsOnlyClientIntent()
    {
        var properties = typeof(CreateBookingRequest).GetProperties().Select(property => property.Name).ToArray();

        Assert.Equal(["ServiceId", "StartTime"], properties);
    }

    [Fact]
    public void BookingEntity_HasCanonicalOwnershipAndConcurrencyFields()
    {
        var names = typeof(Booking).GetProperties().Select(property => property.Name).ToHashSet();

        Assert.Contains("BusinessId", names);
        Assert.Contains("ProviderProfileId", names);
        Assert.Contains("Version", names);
        Assert.Contains("UpdatedAt", names);
    }
}