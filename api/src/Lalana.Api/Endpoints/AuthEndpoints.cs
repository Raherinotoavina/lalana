using Lalana.Application.Auth;
using Lalana.Domain.Identity;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Lalana.Api.Endpoints;

/// <summary>Sign-up, confirmation, sign-in, sessions and password reset (all anonymous).</summary>
internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var auth = routes.MapGroup("/auth").WithTags("Auth").AllowAnonymous();

        auth.MapPost("/sign-up", SignUp)
            .WithName("SignUp")
            .WithSummary("Creates an account and sends a confirmation code (SMS when a phone is given, else e-mail).");
        auth.MapPost("/codes", SendCode)
            .WithName("SendConfirmationCode")
            .WithSummary("Sends a new confirmation code for an unconfirmed phone or e-mail.");
        auth.MapPost("/confirm", Confirm)
            .WithName("ConfirmCode")
            .WithSummary("Confirms the phone or e-mail the code was sent to and signs the member in.");
        auth.MapPost("/sign-in", SignIn)
            .WithName("SignIn")
            .WithSummary("Signs in with a confirmed phone or e-mail and the password.");
        auth.MapPost("/refresh", Refresh)
            .WithName("RefreshSession")
            .WithSummary("Exchanges a refresh token for new tokens; an old token ends the session.");
        auth.MapPost("/sign-out", SignOut)
            .WithName("SignOut")
            .WithSummary("Ends the session of this refresh token.");
        auth.MapPost("/password/forgot", ForgotPassword)
            .WithName("ForgotPassword")
            .WithSummary("Sends a reset code; the answer is the same whether or not an account exists.");
        auth.MapPost("/password/verify", VerifyResetCode)
            .WithName("VerifyResetCode")
            .WithSummary("Checks the reset code and returns a ticket for the new password.");
        auth.MapPost("/password/reset", ResetPassword)
            .WithName("ResetPassword")
            .WithSummary("Sets the new password, ends the other sessions and signs the member in.");
        return routes;
    }

    internal static async Task<Results<Created<VerificationResponse>, ProblemHttpResult>> SignUp(
        SignUpRequest request,
        AccountAuth auth,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var result = await auth.SignUpAsync(
            new SignUpCommand(request.FirstName, request.LastName, request.Phone, request.Email, request.Password),
            cancellationToken);
        return result.Value is { } code
            ? TypedResults.Created((string?)null, VerificationResponse.From(code))
            : result.Failure!.ToProblem(context);
    }

    internal static async Task<Results<Ok<VerificationResponse>, ProblemHttpResult>> SendCode(
        SendCodeRequest request,
        AccountAuth auth,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        IdentifierKind? channel = request.Channel switch
        {
            CodeChannel.Sms => IdentifierKind.Phone,
            CodeChannel.Email => IdentifierKind.Email,
            _ => null,
        };
        var result = await auth.SendConfirmationAsync(request.Identifier, channel, cancellationToken);
        return result.Value is { } code ? TypedResults.Ok(VerificationResponse.From(code)) : result.Failure!.ToProblem(context);
    }

    internal static async Task<Results<Ok<SessionResponse>, ProblemHttpResult>> Confirm(
        ConfirmCodeRequest request,
        AccountAuth auth,
        HttpContext context,
        CancellationToken cancellationToken) =>
        ToSession(await auth.ConfirmAsync(request.VerificationId, request.Code, cancellationToken), context);

    internal static async Task<Results<Ok<SessionResponse>, ProblemHttpResult>> SignIn(
        SignInRequest request,
        AccountAuth auth,
        HttpContext context,
        CancellationToken cancellationToken) =>
        ToSession(await auth.SignInAsync(request.Identifier, request.Password, cancellationToken), context);

    internal static async Task<Results<Ok<SessionResponse>, ProblemHttpResult>> Refresh(
        RefreshRequest request,
        Sessions sessions,
        HttpContext context,
        CancellationToken cancellationToken) =>
        ToSession(await sessions.RefreshAsync(request.RefreshToken, cancellationToken), context);

    internal static async Task<NoContent> SignOut(RefreshRequest request, Sessions sessions, CancellationToken cancellationToken)
    {
        await sessions.EndAsync(request.RefreshToken, cancellationToken);
        return TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<VerificationResponse>, ProblemHttpResult>> ForgotPassword(
        ForgotPasswordRequest request,
        PasswordReset reset,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var result = await reset.RequestAsync(request.Identifier, cancellationToken);
        return result.Value is { } code ? TypedResults.Ok(VerificationResponse.From(code)) : result.Failure!.ToProblem(context);
    }

    internal static async Task<Results<Ok<ResetTicketResponse>, ProblemHttpResult>> VerifyResetCode(
        ConfirmCodeRequest request,
        PasswordReset reset,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var result = await reset.VerifyCodeAsync(request.VerificationId, request.Code, cancellationToken);
        return result.Value is { } ticket
            ? TypedResults.Ok(new ResetTicketResponse(ticket.Ticket, ticket.ExpiresAt))
            : result.Failure!.ToProblem(context);
    }

    internal static async Task<Results<Ok<SessionResponse>, ProblemHttpResult>> ResetPassword(
        ResetPasswordRequest request,
        PasswordReset reset,
        HttpContext context,
        CancellationToken cancellationToken) =>
        ToSession(await reset.ResetAsync(request.ResetTicket, request.NewPassword, cancellationToken), context);

    private static Results<Ok<SessionResponse>, ProblemHttpResult> ToSession(AuthResult<AuthSession> result, HttpContext context) =>
        result.Value is { } session ? TypedResults.Ok(SessionResponse.From(session)) : result.Failure!.ToProblem(context);
}
