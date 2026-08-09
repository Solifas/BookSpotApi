using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BookSpot.API.Swagger;

public class SwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add authorization information
        var hasAuthorize = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<AuthorizeAttribute>()
            .Any() ?? false;

        if (hasAuthorize)
        {
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized - Invalid or missing JWT token" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden - Insufficient permissions" });
        }

        // Add common error responses
        operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Bad Request - Validation errors or invalid input" });
        operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Internal Server Error" });

        // Add specific responses based on HTTP method
        switch (context.ApiDescription.HttpMethod?.ToUpper())
        {
            case "GET":
                if (operation.OperationId?.Contains("Get") == true && !operation.OperationId.Contains("GetAll"))
                {
                    operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Not Found - Resource does not exist" });
                }
                break;
            case "POST":
                operation.Responses.TryAdd("201", new OpenApiResponse { Description = "Created - Resource successfully created" });
                break;
            case "PUT":
                operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Not Found - Resource does not exist" });
                break;
            case "DELETE":
                operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Not Found - Resource does not exist" });
                operation.Responses.TryAdd("204", new OpenApiResponse { Description = "No Content - Resource successfully deleted" });
                break;
        }

        // Add operation descriptions based on controller and action
        AddOperationDescriptions(operation, context);
    }

    private static void AddOperationDescriptions(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerName = context.MethodInfo.DeclaringType?.Name.Replace("Controller", "");
        var actionName = context.MethodInfo.Name;

        operation.Summary = GetOperationSummary(controllerName, actionName);
        operation.Description = GetOperationDescription(controllerName, actionName);

        // Add tags
        if (!string.IsNullOrEmpty(controllerName))
        {
            operation.Tags = new List<OpenApiTag> { new() { Name = controllerName } };
        }
    }

    private static string GetOperationSummary(string? controllerName, string actionName)
    {
        return (controllerName, actionName) switch
        {
            ("Auth", "Register") => "Register a new user",
            ("Auth", "Login") => "Authenticate user and get JWT token",
            ("Profiles", "Get") => "Get user profile by ID",
            ("Profiles", "GetCurrentUser") => "Get current authenticated user's profile",
            ("Businesses", "Get") => "Get business by ID",
            ("Businesses", "GetServices") => "Get services offered by business",
            ("Businesses", "Post") => "Create a new business",
            ("Services", "GetAll") => "Get all available services",
            ("Services", "Search") => "Search services with filters",
            ("Services", "Get") => "Get service by ID",
            ("Services", "Post") => "Create a new service",
            ("Bookings", "Get") => "Get booking by ID",
            ("Bookings", "Post") => "Create a new booking",
            ("BusinessHours", "Get") => "Get business hours by ID",
            ("BusinessHours", "Post") => "Set business hours",
            ("Reviews", "Get") => "Get review by ID",
            ("Reviews", "Post") => "Create a new review",
            _ => $"{actionName} {controllerName}"
        };
    }

    private static string GetOperationDescription(string? controllerName, string actionName)
    {
        return (controllerName, actionName) switch
        {
            ("Auth", "Register") => "Creates a new user account with email, full name, optional contact number, password, and user type (client or provider).",
            ("Auth", "Login") => "Authenticates a user with email and password, returns a JWT token for subsequent API calls.",
            ("Profiles", "Get") => "Retrieves a user profile by ID. Users can only access their own profile unless they are providers.",
            ("Profiles", "GetCurrentUser") => "Retrieves the profile of the currently authenticated user based on JWT token.",
            ("Businesses", "GetServices") => "Retrieves all services offered by a specific business. Useful for browsing services from a particular provider.",
            ("Businesses", "Post") => "Creates a new business for the authenticated provider. Only users with 'provider' type can create businesses.",
            ("Services", "GetAll") => "Retrieves all available services across all businesses.",
            ("Services", "Search") => "Search and filter services by name, city, price range, duration, with pagination support.",
            ("Services", "Post") => "Creates a new service for a business. Only the business owner can create services.",
            ("Bookings", "Post") => "Creates a new booking for a service. Only clients can create bookings. The system automatically determines the provider from the service.",
            ("BusinessHours", "Post") => "Sets operating hours for a specific day of the week for a business. Only business owners can set hours.",
            ("Reviews", "Post") => "Creates a review for a completed booking. Reviews help other clients make informed decisions.",
            _ => $"Performs {actionName} operation on {controllerName}"
        };
    }
}