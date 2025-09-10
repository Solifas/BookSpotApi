using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Infrastructure.Repositories.DynamoDb;
using BookSpot.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add HTTP Context Accessor for Claims Service
builder.Services.AddHttpContextAccessor();

// Add ProblemDetails support
builder.Services.AddProblemDetails();

// Add global exception handler
builder.Services.AddExceptionHandler<BookSpot.Infrastructure.Middleware.GlobalExceptionHandler>();

// Add JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "your-super-secret-jwt-key-that-should-be-at-least-32-characters-long";
var key = Encoding.ASCII.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "BookSpot",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "BookSpot",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IClaimsService, ClaimsService>();

builder.Services.AddMediatR(typeof(BookSpot.Application.Features.Bookings.Commands.CreateBookingCommand).Assembly);

var app = builder.Build();

// Configure exception handling
app.UseExceptionHandler();

// Configure Swagger for development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
