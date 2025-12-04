using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using BookSpot.API.Swagger;
using BookSpot.Application;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Infrastructure.Repositories.DynamoDb;
using BookSpot.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("BookSpotCorsPolicy", policy =>
    {
        // Allow all origins and add Private Network Access headers
        Console.WriteLine("🔧 CORS: Allowing all origins with Private Network Access support");
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Access-Control-Allow-Private-Network");
    });
});

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

builder.Services.AddAuthorization(options =>
{
    // Policy for clients only
    options.AddPolicy("ClientOnly", policy =>
        policy.RequireClaim("user_type", "client"));

    // Policy for providers only
    options.AddPolicy("ProviderOnly", policy =>
        policy.RequireClaim("user_type", "provider"));

    // Policy for both clients and providers
    options.AddPolicy("ClientOrProvider", policy =>
        policy.RequireClaim("user_type", "client", "provider"));
});

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BookSpot API",
        Version = "v1",
        Description = "A comprehensive booking system API for service providers and clients",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "BookSpot Support",
            Email = "support@bookspot.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Configure schema generation
    options.SchemaFilter<SwaggerSchemaFilter>();
    options.OperationFilter<SwaggerOperationFilter>();
});

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

// Configure AWS SES
builder.Services.AddSingleton<Amazon.SimpleEmail.IAmazonSimpleEmailService, Amazon.SimpleEmail.AmazonSimpleEmailServiceClient>();

builder.Services.AddScoped<IDynamoDBContext>(sp => new DynamoDBContext(sp.GetRequiredService<IAmazonDynamoDB>()));

// Repositories (Clean Architecture: Infrastructure behind interfaces)
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBusinessHourRepository, BusinessHourRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IClaimsService, ClaimsService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Add Application layer services (MediatR, FluentValidation, Behaviors)
builder.Services.AddApplication();

var app = builder.Build();

// Configure exception handling
app.UseExceptionHandler();

// Add Private Network Access middleware
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers["Origin"].FirstOrDefault();
    Console.WriteLine($"🌐 Request from origin: {origin ?? "null"}");
    Console.WriteLine($"🔍 Request method: {context.Request.Method}");
    Console.WriteLine($"📍 Request path: {context.Request.Path}");

    // Handle Private Network Access preflight requests
    if (context.Request.Method == "OPTIONS" &&
        context.Request.Headers.ContainsKey("Access-Control-Request-Private-Network"))
    {
        Console.WriteLine("🔒 Private Network Access preflight request detected");
        context.Response.Headers.Add("Access-Control-Allow-Private-Network", "true");
    }

    // Always add Private Network Access header for requests from public origins to localhost
    if (!string.IsNullOrEmpty(origin) && origin.StartsWith("https://") &&
        (context.Request.Host.Host == "localhost" || context.Request.Host.Host == "127.0.0.1"))
    {
        Console.WriteLine("🔓 Adding Private Network Access header");
        context.Response.Headers.Add("Access-Control-Allow-Private-Network", "true");
    }

    await next();

    Console.WriteLine($"✅ Response status: {context.Response.StatusCode}");
    var corsHeaders = context.Response.Headers.Where(h => h.Key.StartsWith("Access-Control"));
    foreach (var header in corsHeaders)
    {
        Console.WriteLine($"🔒 CORS Header: {header.Key} = {header.Value}");
    }
});

// Configure CORS (must be before authentication/authorization)
app.UseCors("BookSpotCorsPolicy");


app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "BookSpot API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "BookSpot API Documentation";
    options.DefaultModelsExpandDepth(2);
    options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
    options.DisplayRequestDuration();
    options.EnableDeepLinking();
    options.EnableFilter();
    options.ShowExtensions();
});

// Configure authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
