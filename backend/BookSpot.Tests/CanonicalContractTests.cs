using System.Reflection;
using BookSpot.API.Controllers;
using BookSpot.Application.DTOs.Canonical;
using BookSpot.Application.Features.Availability;
using BookSpot.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Text.Json;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using BookSpot.Application.Features.Businesses.Commands;
using BookSpot.Application.Features.Services.Commands;
using MediatR;
using Moq;

namespace BookSpot.Tests;

public class CanonicalContractTests
{
    [Theory]
    [InlineData(typeof(BusinessesController), "GetMine", "mine", "GET")]
    [InlineData(typeof(BookingsController), "GetMyClientBookings", "client/me", "GET")]
    [InlineData(typeof(BookingsController), "GetMyProviderBookings", "provider/me", "GET")]
    [InlineData(typeof(DashboardController), "GetMine", "me", "GET")]
    [InlineData(typeof(ServicesController), "GetAvailability", "{id}/availability", "GET")]
    [InlineData(typeof(BusinessesController), "Patch", "{id}", "PATCH")]
    [InlineData(typeof(ServicesController), "Patch", "{id}", "PATCH")]
    [InlineData(typeof(ReviewsController), "Patch", "{id}", "PATCH")]
    public void CanonicalRoute_IsMapped(Type controller, string action, string template, string method)
    {
        var actionMethod = controller.GetMethod(action, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(actionMethod);
        var route = Assert.IsAssignableFrom<HttpMethodAttribute>(actionMethod!.GetCustomAttributes()
            .Single(attribute => attribute is HttpMethodAttribute));
        Assert.Equal(template, route.Template);
        Assert.Contains(method, route.HttpMethods);
    }

    [Fact]
    public void CanonicalDtoMapper_UsesContractNamesAndServerValues()
    {
        var business = new Business
        {
            Id = "business-1", ProviderId = "provider-1", BusinessName = "Studio", Description = "Desc",
            Address = "1 Main", City = "Cape Town", Phone = "+27210000000", Email = "studio@example.com",
            IsActive = true, Rating = 4.5, ReviewCount = 2, CreatedAt = DateTime.UnixEpoch
        };

        var dto = CanonicalDtoMapper.ToBusinessDto(business);

        Assert.Equal("business-1", dto.BusinessId);
        Assert.Equal("provider-1", dto.ProviderProfileId);
        Assert.Equal("Africa/Johannesburg", dto.TimeZone);
        Assert.DoesNotContain(dto.GetType().GetProperties(), property => property.Name == "Id" || property.Name == "ProviderId");
    }

    [Fact]
    public void AvailabilityCalculator_UsesBusinessHoursAndExcludesLiveBookings()
    {
        var service = new Service { Id = "service-1", BusinessId = "business-1", DurationMinutes = 30 };
        var hours = new[]
        {
            new BusinessHour { BusinessId = "business-1", DayOfWeek = (int)DayOfWeek.Monday, OpenTime = "09:00", CloseTime = "11:00" }
        };
        var bookings = new[]
        {
            new Booking { StartTime = new DateTime(2026, 8, 17, 7, 30, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 8, 17, 8, 0, 0, DateTimeKind.Utc), Status = "pending" }
        };

        var result = AvailabilityCalculator.Calculate(service, hours, bookings,
            new DateTimeOffset(2026, 8, 17, 7, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 17, 9, 0, 0, TimeSpan.Zero));

        Assert.Equal(new[] { "2026-08-17T07:00:00.0000000+00:00", "2026-08-17T08:00:00.0000000+00:00", "2026-08-17T08:15:00.0000000+00:00", "2026-08-17T08:30:00.0000000+00:00" },
            result.Slots.Select(slot => slot.StartTime.ToString("O")));
    }

    [Fact]
    public void BookingDto_ClientAndProviderViewsSerializeExactPartySpecificProperties()
    {
        var booking = new Booking
        {
            Id = "booking-1", ServiceId = "service-1", BusinessId = "business-1", ClientId = "client-1",
            Status = "pending", StartTime = DateTime.UnixEpoch, EndTime = DateTime.UnixEpoch.AddMinutes(30),
            CreatedAt = DateTime.UnixEpoch, UpdatedAt = DateTime.UnixEpoch, Version = 1, PriceAmount = 100m
        };
        var service = new Service { Id = "service-1", Name = "Cut", DurationMinutes = 30 };
        var business = new Business { Id = "business-1", ProviderId = "provider-1", BusinessName = "Studio" };
        var client = new Profile { Id = "client-1", FullName = "Client", Email = "client@example.com" };
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        using var clientJson = JsonDocument.Parse(JsonSerializer.Serialize(
            CanonicalDtoMapper.ToBookingDto(booking, service, business, client, "client"), options));
        using var providerJson = JsonDocument.Parse(JsonSerializer.Serialize(
            CanonicalDtoMapper.ToBookingDto(booking, service, business, client, "provider"), options));

        Assert.True(clientJson.RootElement.TryGetProperty("clientProfileId", out _));
        Assert.False(clientJson.RootElement.TryGetProperty("client", out _));
        Assert.True(providerJson.RootElement.TryGetProperty("client", out _));
        Assert.False(providerJson.RootElement.TryGetProperty("clientProfileId", out _));
    }

    [Fact]
    public void BookingDto_PrefersImmutableSnapshotsAndFallsBackForLegacyRows()
    {
        var service = new Service { Id = "service-1", Name = "Current", DurationMinutes = 60, Price = 250m };
        var business = new Business
        {
            Id = "business-1", ProviderId = "provider-1", BusinessName = "Current Business",
            Address = "Current Address", City = "Current City"
        };
        var client = new Profile { Id = "client-1", FullName = "Client" };
        var booking = new Booking
        {
            Id = "booking-1", ServiceId = service.Id, BusinessId = business.Id, ClientId = client.Id,
            StartTime = DateTime.UnixEpoch, EndTime = DateTime.UnixEpoch.AddMinutes(30),
            CreatedAt = DateTime.UnixEpoch, UpdatedAt = DateTime.UnixEpoch,
            ServiceNameSnapshot = "Booked Service", DurationMinutesSnapshot = 30, PriceAmount = 100m,
            BusinessNameSnapshot = "Booked Business", BusinessAddressSnapshot = "Booked Address",
            BusinessCitySnapshot = "Booked City", ProviderProfileId = "provider-1",
            ClientFullNameSnapshot = "Booked Client", ClientEmailSnapshot = "booked@example.com",
            ClientPhoneSnapshot = "+27110000000"
        };

        var captured = Assert.IsType<ClientBookingDto>(
            CanonicalDtoMapper.ToBookingDto(booking, null, null, null, "client"));
        Assert.Equal("Booked Service", captured.Service.Name);
        Assert.Equal("Booked Business", captured.Business.BusinessName);
        Assert.Equal(100m, captured.Price.Amount);
        var providerCaptured = Assert.IsType<ProviderBookingDto>(
            CanonicalDtoMapper.ToBookingDto(booking, null, null, null, "provider"));
        Assert.Equal("Booked Client", providerCaptured.Client.FullName);

        booking.ServiceNameSnapshot = null;
        booking.BusinessNameSnapshot = null;
        booking.PriceAmount = null;
        var legacy = Assert.IsType<ClientBookingDto>(
            CanonicalDtoMapper.ToBookingDto(booking, service, business, client, "client"));
        Assert.Equal("Current", legacy.Service.Name);
        Assert.Equal("Current Business", legacy.Business.BusinessName);
        Assert.Equal(250m, legacy.Price.Amount);
    }

    [Fact]
    public async Task BookingCreate_RejectsMalformedIdempotencyKeyBeforeDispatch()
    {
        var mediator = new Mock<IMediator>();
        var claims = new Mock<IClaimsService>();
        var controller = new BookingsController(mediator.Object, claims.Object);

        var error = await Assert.ThrowsAsync<ValidationException>(() => controller.Post(
            new BookSpot.Application.DTOs.Bookings.CreateBookingRequest(
                "service-1", DateTimeOffset.UtcNow.AddHours(1)), "short"));

        Assert.Equal("invalid_idempotency_key", error.Errors["Idempotency-Key"][0]);
        mediator.Verify(value => value.Send(
            It.IsAny<BookSpot.Application.Features.Bookings.Commands.CreateBookingCommand>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(typeof(BookingsController), "Get")]
    [InlineData(typeof(BookingsController), "GetClientBookings")]
    [InlineData(typeof(BookingsController), "GetProviderBookings")]
    [InlineData(typeof(BusinessesController), "GetServices")]
    [InlineData(typeof(BusinessesController), "GetServicesByProvider")]
    [InlineData(typeof(BusinessesController), "Put")]
    [InlineData(typeof(ReviewsController), "Get")]
    [InlineData(typeof(ReviewsController), "Post")]
    [InlineData(typeof(ReviewsController), "Put")]
    public void ApiActions_DoNotExposePersistenceEntities(Type controller, string action)
    {
        var method = Assert.Single(controller.GetMethods(BindingFlags.Instance | BindingFlags.Public),
            candidate => candidate.Name == action);

        Assert.DoesNotContain("BookSpot.Domain.Entities", method.ReturnType.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task BusinessPatch_RejectsEmptyPatchBeforeDispatch()
    {
        var mediator = new Mock<IMediator>();
        var controller = new BusinessesController(mediator.Object, Mock.Of<IClaimsService>());

        await Assert.ThrowsAsync<ValidationException>(() =>
            controller.Patch("business-1", new UpdateBusinessRequest()));
    }

    [Fact]
    public async Task ServicePatch_RejectsEmptyPatchBeforeDispatch()
    {
        var mediator = new Mock<IMediator>();
        var controller = new ServicesController(mediator.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            controller.Patch("service-1", new UpdateServiceRequest()));
    }

    [Fact]
    public async Task ReviewPatch_RejectsOutOfRangeRatingBeforeDispatch()
    {
        var mediator = new Mock<IMediator>();
        var controller = new ReviewsController(mediator.Object);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.Patch("review-1", new UpdateReviewRequest { Rating = 6 }));

        Assert.Equal("out_of_range", exception.Errors["rating"].Single());
    }

    [Fact]
    public void CanonicalPatchValidators_AcceptOpaqueResourceIds()
    {
        var businessResult = new UpdateBusinessCommandValidator().Validate(
            new UpdateBusinessCommand("local-business-001", BusinessName: "Valid Studio"));
        var serviceResult = new UpdateServiceCommandValidator().Validate(
            new UpdateServiceCommand("local-service-001", Name: "Valid Service"));

        Assert.True(businessResult.IsValid, string.Join("; ", businessResult.Errors));
        Assert.True(serviceResult.IsValid, string.Join("; ", serviceResult.Errors));
    }

    [Fact]
    public void CanonicalServiceCreateValidator_AcceptsOpaqueBusinessIdAndBoundaryValues()
    {
        var result = new CreateServiceCommandValidator().Validate(new CreateServiceCommand(
            "local-business-001", "S", "D", null, 0m, 15));

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public async Task ServiceSearch_RejectsPageAboveContractLimitBeforeDispatch()
    {
        var controller = new ServicesController(Mock.Of<IMediator>());

        await Assert.ThrowsAsync<ValidationException>(() => controller.Search(page: 100001));
    }

    [Fact]
    public void GeneratedOpenApi_DeclaresCanonicalPolymorphicResponses()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../..", "openapi-live.json"));
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        Assert.Equal("view", schemas.GetProperty("BookingDto").GetProperty("discriminator")
            .GetProperty("propertyName").GetString());
        Assert.Equal("kind", schemas.GetProperty("DashboardDto").GetProperty("discriminator")
            .GetProperty("propertyName").GetString());
        Assert.True(schemas.TryGetProperty("ClientBookingDto", out _));
        Assert.True(schemas.TryGetProperty("ProviderBookingDto", out _));
        Assert.True(schemas.TryGetProperty("ClientDashboardDto", out _));
        Assert.True(schemas.TryGetProperty("ProviderDashboardDto", out _));

        var paths = document.RootElement.GetProperty("paths");
        foreach (var route in new[] { "/businesses/mine", "/bookings/client/me", "/bookings/provider/me",
                     "/dashboard/me", "/services/{id}/availability", "/businesses/{id}", "/services/{id}",
                     "/reviews/{id}" })
        {
            foreach (var operation in paths.GetProperty(route).EnumerateObject())
            {
                foreach (var response in operation.Value.GetProperty("responses").EnumerateObject()
                             .Where(value => value.Name is "400" or "401" or "403" or "404" or "409" or "500" or "503"))
                    Assert.True(response.Value.GetProperty("content").TryGetProperty("application/problem+json", out _),
                        $"{route} {operation.Name} {response.Name} must declare application/problem+json");
            }
        }
    }
}
