using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FITSync.WebAPI.Swagger;

public class SwaggerBearerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authAttributes = context.MethodInfo.GetCustomAttributes(true)
            .Union(context.MethodInfo.DeclaringType?.GetCustomAttributes(true) ?? Array.Empty<object>())
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();

        if (!authAttributes.Any())
            return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = Array.Empty<string>()
        });
    }
}
