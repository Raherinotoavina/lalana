using Lalana.Application.Abstractions;
using Lalana.Application.Account;
using Lalana.Application.Auth;
using Lalana.Application.Profiles;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Lalana.Api.Endpoints;

/// <summary>The caller's account.</summary>
/// <param name="Id">The member's id.</param>
/// <param name="FirstName">Prénom.</param>
/// <param name="LastName">Nom.</param>
/// <param name="Phone">E.164 phone number, if any.</param>
/// <param name="PhoneConfirmed">Whether the phone was confirmed by a code.</param>
/// <param name="Email">E-mail address, if any.</param>
/// <param name="EmailConfirmed">Whether the e-mail was confirmed by a code.</param>
/// <param name="HasProfile">False until the profile is created (POST /profiles/bootstrap).</param>
public sealed record MeResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? Phone,
    bool PhoneConfirmed,
    string? Email,
    bool EmailConfirmed,
    bool HasProfile);

/// <summary>Names for the profile; when empty, the names given at sign-up are used.</summary>
/// <param name="FirstName">Prénom.</param>
/// <param name="LastName">Nom.</param>
public sealed record BootstrapProfileRequest(string? FirstName, string? LastName);

/// <summary>A member's public profile.</summary>
/// <param name="Id">The member's id.</param>
/// <param name="FirstName">Prénom.</param>
/// <param name="LastName">Nom.</param>
/// <param name="CreatedAt">When the profile was created.</param>
public sealed record ProfileResponse(Guid Id, string FirstName, string LastName, DateTimeOffset CreatedAt);

/// <summary>A phone or e-mail to add to the account, or to use instead of the current one.</summary>
/// <param name="Identifier">Phone or e-mail.</param>
public sealed record ChangeIdentifierRequest(string? Identifier);

internal static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/me", GetMe)
            .WithName("GetMe")
            .WithTags("Account")
            .WithSummary("Returns the signed-in member's account.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        routes.MapPost("/profiles/bootstrap", Bootstrap)
            .WithName("BootstrapProfile")
            .WithTags("Account")
            .WithSummary("Creates the caller's profile once; later calls return the same profile.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        routes.MapPost("/me/identifiers", RequestIdentifierChange)
            .WithName("RequestIdentifierChange")
            .WithTags("Account")
            .WithSummary("Sends a code to a phone or e-mail to add to the account or to replace the current one.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        routes.MapPost("/me/identifiers/confirm", ConfirmIdentifierChange)
            .WithName("ConfirmIdentifierChange")
            .WithTags("Account")
            .WithSummary("Confirms the code and saves the new phone or e-mail.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        return routes;
    }

    internal static async Task<Results<Ok<MeResponse>, UnauthorizedHttpResult>> GetMe(
        GetMeHandler handler,
        CancellationToken cancellationToken) =>
        await handler.HandleAsync(cancellationToken) is { } me
            ? TypedResults.Ok(new MeResponse(
                me.Id,
                me.FirstName,
                me.LastName,
                me.Phone,
                me.PhoneConfirmed,
                me.Email,
                me.EmailConfirmed,
                me.HasProfile))
            : TypedResults.Unauthorized();

    internal static async Task<Results<Created<ProfileResponse>, Ok<ProfileResponse>, UnauthorizedHttpResult, ProblemHttpResult>> Bootstrap(
        BootstrapProfileRequest request,
        BootstrapProfile bootstrap,
        ICurrentUser currentUser,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await bootstrap.HandleAsync(userId, request.FirstName, request.LastName, cancellationToken);
        if (result.Failure is { Error: AuthError.AccountNotFound })
        {
            return TypedResults.Unauthorized();
        }

        if (result.Failure is { } failure)
        {
            return failure.ToProblem(context);
        }

        var (profile, created) = result.Value;
        var response = new ProfileResponse(profile.Id.Value, profile.FirstName, profile.LastName, profile.CreatedAt);
        return created ? TypedResults.Created("/me", response) : TypedResults.Ok(response);
    }

    internal static async Task<Results<Ok<VerificationResponse>, UnauthorizedHttpResult, ProblemHttpResult>> RequestIdentifierChange(
        ChangeIdentifierRequest request,
        IdentifierChange change,
        ICurrentUser currentUser,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await change.RequestAsync(userId, request.Identifier, cancellationToken);
        return result.Value is { } code ? TypedResults.Ok(VerificationResponse.From(code)) : result.Failure!.ToProblem(context);
    }

    internal static async Task<Results<NoContent, UnauthorizedHttpResult, ProblemHttpResult>> ConfirmIdentifierChange(
        ConfirmCodeRequest request,
        IdentifierChange change,
        ICurrentUser currentUser,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await change.ConfirmAsync(userId, request.VerificationId, request.Code, cancellationToken);
        return result.Succeeded ? TypedResults.NoContent() : result.Failure!.ToProblem(context);
    }
}
