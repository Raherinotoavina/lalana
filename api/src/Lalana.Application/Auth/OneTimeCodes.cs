using System.Security.Cryptography;
using System.Text;
using Lalana.Application.Messaging;
using Lalana.Domain.Identity;

namespace Lalana.Application.Auth;

/// <summary>A code sent to a member: what the app needs to show the code screen.</summary>
/// <param name="Id">Pass it back with the code.</param>
/// <param name="Channel">Where it went.</param>
/// <param name="MaskedDestination">For example <c>+261 34 ** *** 67</c>.</param>
/// <param name="ResendAvailableAt">When another code may be asked.</param>
/// <param name="ExpiresAt">When this code stops working.</param>
public sealed record SentCode(
    Guid Id,
    IdentifierKind Channel,
    string MaskedDestination,
    DateTimeOffset ResendAvailableAt,
    DateTimeOffset ExpiresAt);

/// <summary>Issues, sends and checks 6-digit codes: hashed, short-lived, limited attempts and sends.</summary>
public sealed class OneTimeCodes(
    IVerificationCodes codes,
    ICodeHasher hasher,
    ISmsSender sms,
    IEmailSender email,
    AuthSettings settings,
    TimeProvider clock)
{
    public async Task<AuthResult<SentCode>> SendAsync(
        CodePurpose purpose,
        LoginIdentifier to,
        UserId? userId,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var sent = await codes.SentSinceAsync(to.Value, now.AddHours(-1), cancellationToken);
        var samePurpose = sent.Where(s => s.Purpose == purpose).Select(s => s.SentAt).ToList();
        if (samePurpose.Count > 0 && samePurpose.Max() + settings.CodeResendDelay > now)
        {
            return new AuthFailure(AuthError.CodeRateLimited, samePurpose.Max() + settings.CodeResendDelay - now);
        }

        if (sent.Count >= settings.CodesPerHour)
        {
            return new AuthFailure(AuthError.CodeRateLimited, sent.Min(s => s.SentAt).AddHours(1) - now);
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
        var stored = new StoredCode(
            Guid.CreateVersion7(),
            purpose,
            to.Value,
            userId?.Value,
            hasher.Hash(code),
            now,
            now + settings.CodeLifetime,
            0,
            null);
        await codes.AddAsync(stored, cancellationToken);
        await DeliverAsync(purpose, to, code, cancellationToken);

        return new SentCode(stored.Id, to.Kind, to.Masked, now + settings.CodeResendDelay, stored.ExpiresAt);
    }

    /// <summary>A code that looks sent, for answers that must not reveal whether an account exists.</summary>
    public SentCode Pretend(LoginIdentifier to)
    {
        var now = clock.GetUtcNow();
        return new SentCode(Guid.CreateVersion7(), to.Kind, to.Masked, now + settings.CodeResendDelay, now + settings.CodeLifetime);
    }

    /// <summary>Checks the code and consumes it when it matches.</summary>
    public async Task<AuthResult<StoredCode>> VerifyAsync(
        Guid id,
        CodePurpose purpose,
        string? code,
        CancellationToken cancellationToken,
        UserId? owner = null)
    {
        var stored = await codes.FindAsync(id, cancellationToken);
        if (stored is null
            || stored.Purpose != purpose
            || stored.ConsumedAt is not null
            || (owner is { } expected && stored.UserId != expected.Value))
        {
            return AuthError.CodeInvalid;
        }

        var now = clock.GetUtcNow();
        if (stored.ExpiresAt <= now)
        {
            return AuthError.CodeExpired;
        }

        if (stored.FailedAttempts >= settings.CodeMaxAttempts)
        {
            return AuthError.CodeAttemptsExceeded;
        }

        if (!Matches(stored.Hash, code))
        {
            await codes.RecordFailedAttemptAsync(stored.Id, cancellationToken);
            return stored.FailedAttempts + 1 >= settings.CodeMaxAttempts
                ? AuthError.CodeAttemptsExceeded
                : AuthError.CodeInvalid;
        }

        await codes.ConsumeAsync(stored.Id, now, cancellationToken);
        return stored;
    }

    /// <summary>Issues an opaque single-use ticket (32 random bytes) tied to an account.</summary>
    public async Task<(string Ticket, DateTimeOffset ExpiresAt)> IssueTicketAsync(
        UserId userId,
        string destination,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var ticket = Base64Url(RandomNumberGenerator.GetBytes(32));
        var stored = new StoredCode(
            Guid.CreateVersion7(),
            CodePurpose.ResetTicket,
            destination,
            userId.Value,
            hasher.Hash(ticket),
            now,
            now + settings.ResetTicketLifetime,
            0,
            null);
        await codes.AddAsync(stored, cancellationToken);
        return ($"{stored.Id:N}.{ticket}", stored.ExpiresAt);
    }

    /// <summary>Consumes a ticket; returns its account, or null when it is unknown, used or expired.</summary>
    public async Task<UserId?> RedeemTicketAsync(string? ticket, CancellationToken cancellationToken)
    {
        var parts = ticket?.Split('.', 2);
        if (parts is not [var idText, var secret] || !Guid.TryParseExact(idText, "N", out var id))
        {
            return null;
        }

        var stored = await codes.FindAsync(id, cancellationToken);
        var now = clock.GetUtcNow();
        if (stored is not { Purpose: CodePurpose.ResetTicket, ConsumedAt: null, UserId: { } userId }
            || stored.ExpiresAt <= now
            || !Matches(stored.Hash, secret))
        {
            return null;
        }

        await codes.ConsumeAsync(stored.Id, now, cancellationToken);
        return UserId.From(userId);
    }

    private bool Matches(string expectedHash, string? candidate) =>
        candidate is not null
        && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expectedHash),
            Encoding.ASCII.GetBytes(hasher.Hash(candidate.Trim())));

    private Task DeliverAsync(CodePurpose purpose, LoginIdentifier to, string code, CancellationToken cancellationToken)
    {
        var (subject, text) = purpose switch
        {
            CodePurpose.ResetPassword => (
                "Votre code de réinitialisation lalana",
                $"{code} est votre code lalana pour choisir un nouveau mot de passe. Il expire dans {Minutes} minutes. Ne le partagez avec personne."),
            _ => (
                "Votre code de confirmation lalana",
                $"{code} est votre code de confirmation lalana. Il expire dans {Minutes} minutes. Ne le partagez avec personne."),
        };

        return to.Phone is { } phone
            ? sms.SendAsync(phone, text, cancellationToken)
            : email.SendAsync(to.Email!, subject, text, cancellationToken);
    }

    private int Minutes => (int)settings.CodeLifetime.TotalMinutes;

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
