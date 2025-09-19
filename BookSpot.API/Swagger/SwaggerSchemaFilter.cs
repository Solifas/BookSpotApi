using System.ComponentModel.DataAnnotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BookSpot.API.Swagger;

public class SwaggerSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null) return;

        // Add examples and descriptions based on attributes
        foreach (var property in context.Type.GetProperties())
        {
            var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];

            if (schema.Properties.TryGetValue(propertyName, out var propertySchema))
            {
                // Add required attribute information
                var requiredAttribute = property.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();
                if (requiredAttribute != null)
                {
                    propertySchema.Description = (propertySchema.Description ?? "") + " (Required)";
                }

                // Add examples based on property names
                AddExampleBasedOnPropertyName(propertyName, propertySchema);
            }
        }

        // Add specific examples for known types
        AddTypeSpecificExamples(schema, context.Type.Name);
    }

    private static void AddExampleBasedOnPropertyName(string propertyName, OpenApiSchema propertySchema)
    {
        propertySchema.Example = propertyName.ToLower() switch
        {
            "email" => new Microsoft.OpenApi.Any.OpenApiString("user@example.com"),
            "fullname" => new Microsoft.OpenApi.Any.OpenApiString("John Doe"),
            "contactnumber" => new Microsoft.OpenApi.Any.OpenApiString("+1 (555) 123-4567"),
            "password" => new Microsoft.OpenApi.Any.OpenApiString("SecurePass123!"),
            "usertype" => new Microsoft.OpenApi.Any.OpenApiString("client"),
            "businessname" => new Microsoft.OpenApi.Any.OpenApiString("Sunny Day Spa"),
            "city" => new Microsoft.OpenApi.Any.OpenApiString("New York"),
            "name" => new Microsoft.OpenApi.Any.OpenApiString("Deep Tissue Massage"),
            "price" => new Microsoft.OpenApi.Any.OpenApiDouble(85.50),
            "durationminutes" => new Microsoft.OpenApi.Any.OpenApiInteger(90),
            "dayofweek" => new Microsoft.OpenApi.Any.OpenApiInteger(1),
            "opentime" => new Microsoft.OpenApi.Any.OpenApiString("09:00"),
            "closetime" => new Microsoft.OpenApi.Any.OpenApiString("17:00"),
            "starttime" => new Microsoft.OpenApi.Any.OpenApiString("2024-01-15T14:00:00Z"),
            "endtime" => new Microsoft.OpenApi.Any.OpenApiString("2024-01-15T15:30:00Z"),
            "rating" => new Microsoft.OpenApi.Any.OpenApiInteger(5),
            "comment" => new Microsoft.OpenApi.Any.OpenApiString("Excellent service!"),
            _ => null
        };
    }

    private static void AddTypeSpecificExamples(OpenApiSchema schema, string typeName)
    {
        switch (typeName)
        {
            case "RegisterRequest":
                schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["email"] = new Microsoft.OpenApi.Any.OpenApiString("john.doe@example.com"),
                    ["fullName"] = new Microsoft.OpenApi.Any.OpenApiString("John Doe"),
                    ["contactNumber"] = new Microsoft.OpenApi.Any.OpenApiString("+1 (555) 123-4567"),
                    ["password"] = new Microsoft.OpenApi.Any.OpenApiString("SecurePass123!"),
                    ["userType"] = new Microsoft.OpenApi.Any.OpenApiString("client")
                };
                break;
            case "LoginRequest":
                schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["email"] = new Microsoft.OpenApi.Any.OpenApiString("john.doe@example.com"),
                    ["password"] = new Microsoft.OpenApi.Any.OpenApiString("SecurePass123!")
                };
                break;
        }
    }
}