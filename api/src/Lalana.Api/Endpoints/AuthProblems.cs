using System.Globalization;
using Lalana.Application.Auth;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Lalana.Api.Endpoints;

/// <summary>Maps auth failures to problem details with a stable <c>code</c> the app translates.</summary>
internal static class AuthProblems
{
    public const string CodeKey = "code";

    public static ProblemHttpResult ToProblem(this AuthFailure failure, HttpContext context)
    {
        var (status, code, title) = Describe(failure.Error);
        if (failure.RetryAfter is { } retryAfter)
        {
            context.Response.Headers.RetryAfter =
                Math.Max(1, (long)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        return TypedResults.Problem(
            statusCode: status,
            title: title,
            extensions: new Dictionary<string, object?> { [CodeKey] = code });
    }

    internal static (int Status, string Code, string Title) Describe(AuthError error) => error switch
    {
        AuthError.IdentifierRequired => (400, "identifier_required", "A phone number or an e-mail is required."),
        AuthError.InvalidPhone => (400, "invalid_phone", "Not a Malagasy mobile number."),
        AuthError.InvalidEmail => (400, "invalid_email", "Not an e-mail address."),
        AuthError.NameRequired => (400, "name_required", "First and last name are required."),
        AuthError.WeakPassword => (400, "weak_password", "The password needs 8 characters including a digit."),
        AuthError.PhoneTaken => (409, "phone_taken", "This phone number already has an account."),
        AuthError.EmailTaken => (409, "email_taken", "This e-mail already has an account."),
        AuthError.InvalidCredentials => (401, "invalid_credentials", "Wrong identifier or password."),
        AuthError.AccountLocked => (423, "account_locked", "Too many attempts; try again later."),
        AuthError.AccountNotVerified => (403, "account_not_verified", "This phone or e-mail is not confirmed yet."),
        AuthError.AccountNotFound => (404, "account_not_found", "No account uses this phone or e-mail."),
        AuthError.AlreadyVerified => (409, "already_verified", "This phone or e-mail is already confirmed."),
        AuthError.CodeInvalid => (400, "code_invalid", "Wrong code."),
        AuthError.CodeExpired => (400, "code_expired", "This code has expired."),
        AuthError.CodeAttemptsExceeded => (400, "code_attempts_exceeded", "Too many wrong codes; ask a new one."),
        AuthError.CodeRateLimited => (429, "code_rate_limited", "Too many codes asked; wait before asking again."),
        AuthError.InvalidRefreshToken => (401, "invalid_refresh_token", "The session has ended; sign in again."),
        AuthError.InvalidResetTicket => (400, "invalid_reset_ticket", "The reset has expired; start again."),
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, null),
    };
}
