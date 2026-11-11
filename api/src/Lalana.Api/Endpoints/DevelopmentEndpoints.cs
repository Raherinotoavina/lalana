using Lalana.Infrastructure.Messaging;

namespace Lalana.Api.Endpoints;

/// <summary>Development only: reads the SMS and e-mail the in-memory sender kept, to get codes.</summary>
internal static class DevelopmentEndpoints
{
    public static IEndpointRouteBuilder MapDevelopmentEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/dev/messages", (DevelopmentMessages messages) => messages.All.Reverse())
            .AllowAnonymous()
            .ExcludeFromDescription();
        return routes;
    }
}
