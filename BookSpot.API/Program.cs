using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Infrastructure.Repositories.DynamoDb;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configure AWS DynamoDB
var isDevelopment = builder.Environment.IsDevelopment();
if (isDevelopment)
{
    // LocalStack configuration for development
    var config = new AmazonDynamoDBConfig
    {
        ServiceURL = "http://localhost:4566",
        UseHttp = true
    };
    builder.Services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient("test", "test", config));
}
else
{
    // AWS Lambda configuration for production
    builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);
    builder.Services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
}

builder.Services.AddScoped<IDynamoDBContext>(sp => new DynamoDBContext(sp.GetRequiredService<IAmazonDynamoDB>()));

// Repositories (Clean Architecture: Infrastructure behind interfaces)
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBusinessHourRepository, BusinessHourRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddMediatR(typeof(BookSpot.Application.Features.Bookings.Commands.CreateBookingCommand).Assembly);

var app = builder.Build();

app.MapControllers();

app.Run();
