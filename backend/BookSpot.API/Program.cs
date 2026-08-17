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

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(entry => string.IsNullOrEmpty(entry.Key) ? "$" : entry.Key,
                entry => entry.Value!.Errors.Select(_ => "invalid").ToArray());
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = "https://bookspot.example/problems/validation-failed",
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more request fields are invalid.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["code"] = "validation_failed";
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        problem.Extensions["errors"] = errors;
        return new Microsoft.AspNetCore.Mvc.ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" }
        };
    };
});

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("BookSpotCorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Allow all origins in development for local testing
            Console.WriteLine("🔧 CORS: Development mode - allowing all origins");
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // Use configured origins in production
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "https://d6bnplwpittsd.cloudfront.net" };

            Console.WriteLine($"🔒 CORS: Production mode - allowed origins: {string.Join(", ", allowedOrigins)}");
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Add HTTP Context Accessor for Claims Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);

// Add ProblemDetails support
builder.Services.AddProblemDetails();

// Add global exception handler
builder.Services.AddExceptionHandler<BookSpot.Infrastructure.Middleware.GlobalExceptionHandler>();

// Add JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecretKey) || Encoding.UTF8.GetByteCount(jwtSecretKey) < 32)
{
    throw new InvalidOperationException("Jwt:SecretKey must be configured with at least 32 UTF-8 bytes.");
}
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
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var subject = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var securityVersionValue = context.Principal?.FindFirst("sv")?.Value;
            if (subject is null || !int.TryParse(securityVersionValue, out var securityVersion))
            {
                context.Fail("Invalid session.");
                return;
            }

            var profiles = context.HttpContext.RequestServices.GetRequiredService<IProfileRepository>();
            var profile = await profiles.GetAsync(subject);
            if (profile is null || profile.SecurityVersion != securityVersion)
            {
                context.Fail("Session revoked.");
            }
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
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
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler,
    BookSpot.Infrastructure.Middleware.ProblemDetailsAuthorizationMiddlewareResultHandler>();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.UseOneOfForPolymorphism();
    options.SelectSubTypesUsing(baseType => baseType == typeof(BookSpot.Application.DTOs.Canonical.BookingDto)
        ? [typeof(BookSpot.Application.DTOs.Canonical.ClientBookingDto),
            typeof(BookSpot.Application.DTOs.Canonical.ProviderBookingDto)]
        : baseType == typeof(BookSpot.Application.DTOs.Canonical.DashboardDto)
            ? [typeof(BookSpot.Application.DTOs.Canonical.ClientDashboardDto),
                typeof(BookSpot.Application.DTOs.Canonical.ProviderDashboardDto)]
            : []);
    options.SelectDiscriminatorNameUsing(baseType =>
        baseType == typeof(BookSpot.Application.DTOs.Canonical.BookingDto) ? "view" :
        baseType == typeof(BookSpot.Application.DTOs.Canonical.DashboardDto) ? "kind" : null);
    options.SelectDiscriminatorValueUsing(subType =>
        subType == typeof(BookSpot.Application.DTOs.Canonical.ClientBookingDto) ? "client" :
        subType == typeof(BookSpot.Application.DTOs.Canonical.ProviderBookingDto) ? "provider" :
        subType == typeof(BookSpot.Application.DTOs.Canonical.ClientDashboardDto) ? "client" :
        subType == typeof(BookSpot.Application.DTOs.Canonical.ProviderDashboardDto) ? "provider" : null);
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
    // LocalStack values come from appsettings.Development.json and can be overridden
    // with standard .NET environment variables (for example AWS__Region).
    var serviceUrl = builder.Configuration["AWS:ServiceURL"]
        ?? throw new InvalidOperationException("AWS:ServiceURL is required in Development.");
    var awsRegion = builder.Configuration["AWS:Region"]
        ?? throw new InvalidOperationException("AWS:Region is required in Development.");
    var accessKey = builder.Configuration["AWS:AccessKey"]
        ?? throw new InvalidOperationException("AWS:AccessKey is required in Development.");
    var secretKey = builder.Configuration["AWS:SecretKey"]
        ?? throw new InvalidOperationException("AWS:SecretKey is required in Development.");

    var config = new AmazonDynamoDBConfig
    {
        ServiceURL = serviceUrl,
        AuthenticationRegion = awsRegion,
        UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
    };
    builder.Services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(accessKey, secretKey, config));
}
else
{
    // Production uses the AWS SDK's role/credential chain. No production credentials
    // or DynamoDB endpoint are stored in source control.
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

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/auth") ||
        context.Request.Path.StartsWithSegments("/profiles") ||
        context.Request.Path.StartsWithSegments("/bookings") ||
        context.Request.Path.StartsWithSegments("/dashboard"))
    {
        context.Response.Headers.CacheControl = "no-store, max-age=0";
        context.Response.Headers.Pragma = "no-cache";
    }

    await next();
});

// Add Private Network Access middleware
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers["Origin"].FirstOrDefault();

    // Handle Private Network Access preflight requests
    if (context.Request.Method == "OPTIONS" &&
        context.Request.Headers.ContainsKey("Access-Control-Request-Private-Network"))
    {
        context.Response.Headers["Access-Control-Allow-Private-Network"] = "true";
    }

    // Always add Private Network Access header for requests from public origins to localhost
    if (!string.IsNullOrEmpty(origin) && origin.StartsWith("https://") &&
        (context.Request.Host.Host == "localhost" || context.Request.Host.Host == "127.0.0.1"))
    {
        context.Response.Headers["Access-Control-Allow-Private-Network"] = "true";
    }

    await next();
});

// Configure CORS (must be before authentication/authorization)
app.UseCors("BookSpotCorsPolicy");


if (app.Environment.IsDevelopment())
{
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
}

// Configure authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
