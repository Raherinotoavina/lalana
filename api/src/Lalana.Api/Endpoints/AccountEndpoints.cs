using Lalana.Application.Account;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Lalana.Api.Endpoints;

/// <summary>The caller's account.</summary>
/// <param name="Id">The member's id.</param>
public sealed record MeResponse(Guid Id);

internal static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/me", GetMe)
            .WithName("GetMe")
            .WithTags("Account")
            .WithSummary("Returns the signed-in member's account.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        return routes;
    }

    internal static Results<Ok<MeResponse>, UnauthorizedHttpResult> GetMe(GetMeHandler handler) =>
        handler.Handle() is { } me ? TypedResults.Ok(new MeResponse(me.Id)) : TypedResults.Unauthorized();
}
