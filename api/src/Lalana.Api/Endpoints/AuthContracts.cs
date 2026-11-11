using Lalana.Application.Auth;
using Lalana.Domain.Identity;

namespace Lalana.Api.Endpoints;

/// <summary>Where a code was sent.</summary>
public enum CodeChannel
{
    Sms,
    Email,
}

/// <summary>Sign-up: names, at least one of phone and e-mail, and a password.</summary>
/// <param name="FirstName">Prénom.</param>
/// <param name="LastName">Nom.</param>
/// <param name="Phone">Malagasy mobile number, local (034 12 345 67) or +261 form. Optional if Email is set.</param>
/// <param name="Email">E-mail address. Optional if Phone is set.</param>
/// <param name="Password">8 characters or more, including a digit.</param>
public sealed record SignUpRequest(string? FirstName, string? LastName, string? Phone, string? Email, string? Password);

/// <summary>A code was sent (or, for a password reset, would have been sent).</summary>
/// <param name="VerificationId">Send it back with the code.</param>
/// <param name="Channel">SMS or e-mail.</param>
/// <param name="MaskedDestination">For example "+261 34 ** *** 67" or "s***@exemple.com".</param>
/// <param name="ResendAvailableAt">When another code may be asked.</param>
/// <param name="ExpiresAt">When this code stops working.</param>
public sealed record VerificationResponse(
    Guid VerificationId,
    CodeChannel Channel,
    string MaskedDestination,
    DateTimeOffset ResendAvailableAt,
    DateTimeOffset ExpiresAt)
{
    internal static VerificationResponse From(SentCode code) => new(
        code.Id,
        code.Channel == IdentifierKind.Phone ? CodeChannel.Sms : CodeChannel.Email,
        code.MaskedDestination,
        code.ResendAvailableAt,
        code.ExpiresAt);
}

/// <summary>Asks a new confirmation code for an unconfirmed phone or e-mail.</summary>
/// <param name="Identifier">Phone or e-mail of the account.</param>
/// <param name="Channel">Send to the account's e-mail instead of its phone (or the reverse); defaults to the identifier's own kind.</param>
public sealed record SendCodeRequest(string? Identifier, CodeChannel? Channel);

/// <summary>A code typed by the member.</summary>
/// <param name="VerificationId">From the response that sent the code.</param>
/// <param name="Code">The 6 digits.</param>
public sealed record ConfirmCodeRequest(Guid VerificationId, string? Code);

/// <summary>Sign-in with a phone or an e-mail and the password.</summary>
/// <param name="Identifier">"Téléphone ou email".</param>
/// <param name="Password">The password.</param>
public sealed record SignInRequest(string? Identifier, string? Password);

/// <summary>The member is signed in.</summary>
/// <param name="UserId">The member's id.</param>
/// <param name="AccessToken">Bearer token for API calls (short-lived).</param>
/// <param name="AccessTokenExpiresAt">When to refresh.</param>
/// <param name="RefreshToken">Single use: exchange it for new tokens; keep it in secure storage.</param>
/// <param name="RefreshTokenExpiresAt">When the member must sign in again.</param>
public sealed record SessionResponse(
    Guid UserId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    internal static SessionResponse From(AuthSession session) => new(
        session.UserId.Value,
        session.AccessToken,
        session.AccessTokenExpiresAt,
        session.RefreshToken,
        session.RefreshTokenExpiresAt);
}

/// <summary>A refresh token to exchange or revoke.</summary>
/// <param name="RefreshToken">The refresh token.</param>
public sealed record RefreshRequest(string? RefreshToken);

/// <summary>Starts a password reset.</summary>
/// <param name="Identifier">Phone or e-mail.</param>
public sealed record ForgotPasswordRequest(string? Identifier);

/// <summary>Allows choosing the new password, once.</summary>
/// <param name="ResetTicket">Pass it to the reset call.</param>
/// <param name="ExpiresAt">When it stops working.</param>
public sealed record ResetTicketResponse(string ResetTicket, DateTimeOffset ExpiresAt);

/// <summary>The new password.</summary>
/// <param name="ResetTicket">From the code check.</param>
/// <param name="NewPassword">8 characters or more, including a digit.</param>
public sealed record ResetPasswordRequest(string? ResetTicket, string? NewPassword);
