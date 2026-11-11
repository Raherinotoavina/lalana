using Lalana.Domain.Identity;

namespace Lalana.Application.Auth;

public sealed record SignUpCommand(string? FirstName, string? LastName, string? Phone, string? Email, string? Password);

/// <summary>Sign-up, confirmation and sign-in with a phone or an e-mail and a password.</summary>
public sealed class AccountAuth(IAccounts accounts, OneTimeCodes codes, Sessions sessions)
{
    /// <summary>
    /// Creates an unconfirmed account and sends a code: by SMS when a phone is given (the e-mail,
    /// if any, is confirmed later), otherwise by e-mail.
    /// </summary>
    public async Task<AuthResult<SentCode>> SignUpAsync(SignUpCommand command, CancellationToken cancellationToken)
    {
        if (!PersonName.TryNormalize(command.FirstName, out var firstName)
            || !PersonName.TryNormalize(command.LastName, out var lastName))
        {
            return AuthError.NameRequired;
        }

        PhoneNumber? phone = null;
        if (!string.IsNullOrWhiteSpace(command.Phone) && !PhoneNumber.TryParse(command.Phone, out phone))
        {
            return AuthError.InvalidPhone;
        }

        EmailAddress? email = null;
        if (!string.IsNullOrWhiteSpace(command.Email) && !EmailAddress.TryParse(command.Email, out email))
        {
            return AuthError.InvalidEmail;
        }

        if (phone is null && email is null)
        {
            return AuthError.IdentifierRequired;
        }

        if (!PasswordRules.IsAcceptable(command.Password))
        {
            return AuthError.WeakPassword;
        }

        if (phone is not null && await accounts.FindAsync(LoginIdentifier.Of(phone), cancellationToken) is not null)
        {
            return AuthError.PhoneTaken;
        }

        if (email is not null && await accounts.FindAsync(LoginIdentifier.Of(email), cancellationToken) is not null)
        {
            return AuthError.EmailTaken;
        }

        var account = await accounts.CreateAsync(new NewAccount(firstName, lastName, phone, email), command.Password!, cancellationToken);
        if (account is null)
        {
            return phone is not null ? AuthError.PhoneTaken : AuthError.EmailTaken;
        }

        var first = phone is not null ? LoginIdentifier.Of(phone) : LoginIdentifier.Of(email!);
        return await codes.SendAsync(CodePurpose.Confirm, first, account.Id, cancellationToken);
    }

    /// <summary>
    /// Sends a confirmation code for an unconfirmed identifier. With <paramref name="channel"/> set to
    /// the other kind, the code goes to the account's other identifier (the e-mail when SMS fails).
    /// </summary>
    public async Task<AuthResult<SentCode>> SendConfirmationAsync(
        string? identifierText,
        IdentifierKind? channel,
        CancellationToken cancellationToken)
    {
        var parsed = LoginIdentifier.Parse(identifierText);
        if (parsed.Identifier is not { } identifier)
        {
            return ToError(parsed.Error!.Value);
        }

        var account = await accounts.FindAsync(identifier, cancellationToken);
        if (account is null)
        {
            return AuthError.AccountNotFound;
        }

        var target = (channel ?? identifier.Kind) == identifier.Kind
            ? identifier
            : OtherIdentifier(account, identifier.Kind);
        if (target is null)
        {
            return AuthError.AccountNotFound;
        }

        if (account.IsConfirmed(target))
        {
            return AuthError.AlreadyVerified;
        }

        return await codes.SendAsync(CodePurpose.Confirm, target, account.Id, cancellationToken);
    }

    /// <summary>Confirms the identifier the code was sent to, then signs the member in.</summary>
    public async Task<AuthResult<AuthSession>> ConfirmAsync(Guid verificationId, string? code, CancellationToken cancellationToken)
    {
        var verified = await codes.VerifyAsync(verificationId, CodePurpose.Confirm, code, cancellationToken);
        if (verified.Value is not { UserId: { } ownerId } stored)
        {
            return verified.Failure ?? new AuthFailure(AuthError.CodeInvalid);
        }

        var userId = UserId.From(ownerId);
        if (LoginIdentifier.Parse(stored.Destination).Identifier is not { } identifier
            || await accounts.FindByIdAsync(userId, cancellationToken) is null)
        {
            return AuthError.CodeInvalid;
        }

        await accounts.ConfirmAsync(userId, identifier, cancellationToken);
        return await sessions.StartAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Signs in with a phone or e-mail and the password. The identifier used must be confirmed;
    /// unknown identifiers cost the same time as a wrong password.
    /// </summary>
    public async Task<AuthResult<AuthSession>> SignInAsync(string? identifierText, string? password, CancellationToken cancellationToken)
    {
        var parsed = LoginIdentifier.Parse(identifierText);
        if (parsed.Identifier is not { } identifier)
        {
            return ToError(parsed.Error!.Value);
        }

        password ??= string.Empty;
        var account = await accounts.FindAsync(identifier, cancellationToken);
        if (account is null)
        {
            accounts.CheckPasswordOfNobody(password);
            return AuthError.InvalidCredentials;
        }

        switch (await accounts.CheckPasswordAsync(account.Id, password, cancellationToken))
        {
            case PasswordCheck.LockedOut:
                return AuthError.AccountLocked;
            case PasswordCheck.Wrong:
                return AuthError.InvalidCredentials;
        }

        if (!account.IsConfirmed(identifier))
        {
            return AuthError.AccountNotVerified;
        }

        return await sessions.StartAsync(account.Id, cancellationToken);
    }

    internal static AuthError ToError(IdentifierError error) => error switch
    {
        IdentifierError.InvalidPhone => AuthError.InvalidPhone,
        IdentifierError.InvalidEmail => AuthError.InvalidEmail,
        _ => AuthError.IdentifierRequired,
    };

    private static LoginIdentifier? OtherIdentifier(UserAccount account, IdentifierKind kind) =>
        kind == IdentifierKind.Phone
            ? account.Email is { } email ? LoginIdentifier.Of(email) : null
            : account.Phone is { } phone ? LoginIdentifier.Of(phone) : null;
}

/// <summary>A ticket that allows choosing a new password, after a valid reset code.</summary>
public sealed record ResetTicket(string Ticket, DateTimeOffset ExpiresAt);

/// <summary>
/// Password reset by SMS or e-mail code. The first step answers the same way whether or not an
/// account exists.
/// </summary>
public sealed class PasswordReset(IAccounts accounts, OneTimeCodes codes, Sessions sessions, TimeProvider clock)
{
    public async Task<AuthResult<SentCode>> RequestAsync(string? identifierText, CancellationToken cancellationToken)
    {
        var parsed = LoginIdentifier.Parse(identifierText);
        if (parsed.Identifier is not { } identifier)
        {
            return AccountAuth.ToError(parsed.Error!.Value);
        }

        var account = await accounts.FindAsync(identifier, cancellationToken);
        if (account is null || !account.IsConfirmed(identifier))
        {
            return codes.Pretend(identifier);
        }

        var sent = await codes.SendAsync(CodePurpose.ResetPassword, identifier, account.Id, cancellationToken);
        if (sent.Value is { } code)
        {
            return code;
        }

        // Rate limited: answer like an unknown account, with the real waiting time.
        return codes.Pretend(identifier) with { ResendAvailableAt = clock.GetUtcNow() + (sent.Failure!.RetryAfter ?? TimeSpan.Zero) };
    }

    public async Task<AuthResult<ResetTicket>> VerifyCodeAsync(Guid verificationId, string? code, CancellationToken cancellationToken)
    {
        var verified = await codes.VerifyAsync(verificationId, CodePurpose.ResetPassword, code, cancellationToken);
        if (verified.Value is not { UserId: { } ownerId } stored)
        {
            return verified.Failure ?? new AuthFailure(AuthError.CodeInvalid);
        }

        var (ticket, expiresAt) = await codes.IssueTicketAsync(UserId.From(ownerId), stored.Destination, cancellationToken);
        return new ResetTicket(ticket, expiresAt);
    }

    /// <summary>Sets the new password, ends every other session and signs the member in.</summary>
    public async Task<AuthResult<AuthSession>> ResetAsync(string? ticket, string? newPassword, CancellationToken cancellationToken)
    {
        if (!PasswordRules.IsAcceptable(newPassword))
        {
            return AuthError.WeakPassword;
        }

        if (await codes.RedeemTicketAsync(ticket, cancellationToken) is not { } userId
            || await accounts.FindByIdAsync(userId, cancellationToken) is null)
        {
            return AuthError.InvalidResetTicket;
        }

        await accounts.SetPasswordAsync(userId, newPassword!, cancellationToken);
        await sessions.EndAllAsync(userId, cancellationToken);
        return await sessions.StartAsync(userId, cancellationToken);
    }
}

/// <summary>Adds or changes the phone or e-mail of a signed-in member, confirmed by a code.</summary>
public sealed class IdentifierChange(IAccounts accounts, OneTimeCodes codes)
{
    public async Task<AuthResult<SentCode>> RequestAsync(UserId userId, string? identifierText, CancellationToken cancellationToken)
    {
        var parsed = LoginIdentifier.Parse(identifierText);
        if (parsed.Identifier is not { } identifier)
        {
            return AccountAuth.ToError(parsed.Error!.Value);
        }

        if (await accounts.FindAsync(identifier, cancellationToken) is { } owner)
        {
            if (owner.Id != userId)
            {
                return Taken(identifier);
            }

            if (owner.IsConfirmed(identifier))
            {
                return AuthError.AlreadyVerified;
            }
        }

        return await codes.SendAsync(CodePurpose.ChangeIdentifier, identifier, userId, cancellationToken);
    }

    public async Task<AuthResult<UserAccount>> ConfirmAsync(UserId userId, Guid verificationId, string? code, CancellationToken cancellationToken)
    {
        var verified = await codes.VerifyAsync(verificationId, CodePurpose.ChangeIdentifier, code, cancellationToken, owner: userId);
        if (verified.Value is not { } stored)
        {
            return verified.Failure!;
        }

        var identifier = LoginIdentifier.Parse(stored.Destination).Identifier!;
        if (!await accounts.ReplaceAsync(userId, identifier, cancellationToken))
        {
            return Taken(identifier);
        }

        return (await accounts.FindByIdAsync(userId, cancellationToken))!;
    }

    private static AuthError Taken(LoginIdentifier identifier) =>
        identifier.Kind == IdentifierKind.Phone ? AuthError.PhoneTaken : AuthError.EmailTaken;
}
