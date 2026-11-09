using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Lalana.Api.OpenApi;

internal static class OpenApiTransformers
{
    public const string BearerScheme = "Bearer";

    /// <summary>Title, version and the bearer security scheme of the document.</summary>
    public static Task DescribeDocument(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Lalana API",
            Version = "v1",
            Description = "Carpooling API. Every endpoint requires an access token unless marked otherwise.",
        };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            [BearerScheme] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
            },
        };
        return Task.CompletedTask;
    }

    /// <summary>Marks every operation as requiring the bearer token, except anonymous ones.</summary>
    public static Task RequireBearer(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var anonymous = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any();
        if (!anonymous)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, context.Document)] = [],
            });
        }

        return Task.CompletedTask;
    }
}
