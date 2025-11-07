using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NotificationService.Swagger;

public class OpenApiVersionDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // The Openapi property might not be available in this version
        // Let's try a different approach by ensuring the document structure is correct
    }
}
