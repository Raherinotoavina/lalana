using Lalana.Domain.Identity;

namespace Lalana.Application.Auth;

/// <summary>An account as the use cases see it.</summary>
public sealed record UserAccount(
    UserId Id,
    PhoneNumber? Phone,
    bool PhoneConfirmed,
    EmailAddress? Email,
    bool EmailConfirmed,
    string FirstName,
    string LastName)
{
    public bool IsConfirmed(LoginIdentifier identifier) =>
        identifier.Kind == IdentifierKind.Phone ? PhoneConfirmed : EmailConfirmed;

    public bool HasAnyConfirmedIdentifier => PhoneConfirmed || EmailConfirmed;
}

/// <summary>Data for a new account; the password is passed separately.</summary>
public sealed record NewAccount(string FirstName, string LastName, PhoneNumber? Phone, EmailAddress? Email);

public enum PasswordCheck
{
    Valid,
    Wrong,
    LockedOut,
}

/// <summary>UserAccount store (ASP.NET Core Identity behind it).</summary>
public interface IAccounts
{
    Task<UserAccount?> FindByIdAsync(UserId id, CancellationToken cancellationToken);

    Task<UserAccount?> FindAsync(LoginIdentifier identifier, CancellationToken cancellationToken);

    /// <summary>Creates the account; returns null when the phone or e-mail is already used.</summary>
    Task<UserAccount?> CreateAsync(NewAccount account, string password, CancellationToken cancellationToken);

    /// <summary>Checks the password, counting failures towards the lockout.</summary>
    Task<PasswordCheck> CheckPasswordAsync(UserId id, string password, CancellationToken cancellationToken);

    /// <summary>Spends the same time as a real check, for unknown identifiers.</summary>
    void CheckPasswordOfNobody(string password);

    Task ConfirmAsync(UserId id, LoginIdentifier identifier, CancellationToken cancellationToken);

    /// <summary>Sets and confirms a new phone or e-mail; false when another account uses it.</summary>
    Task<bool> ReplaceAsync(UserId id, LoginIdentifier identifier, CancellationToken cancellationToken);

    Task SetPasswordAsync(UserId id, string password, CancellationToken cancellationToken);
}

/// <summary>What a one-time code or ticket is for.</summary>
public enum CodePurpose
{
    /// <summary>Confirms the phone or e-mail given at sign-up (or later still unconfirmed).</summary>
    Confirm,

    /// <summary>Proves the owner of a phone or e-mail before a password reset.</summary>
    ResetPassword,

    /// <summary>Opaque ticket, issued after a valid reset code, that allows the new password.</summary>
    ResetTicket,

    /// <summary>Confirms a phone or e-mail added or changed from the account settings.</summary>
    ChangeIdentifier,
}

/// <summary>A stored one-time code (only its hash).</summary>
public sealed record StoredCode(
    Guid Id,
    CodePurpose Purpose,
    string Destination,
    Guid? UserId,
    string Hash,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    int FailedAttempts,
    DateTimeOffset? ConsumedAt);

public interface IVerificationCodes
{
    Task<StoredCode?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Codes sent to this destination since the given time, any purpose (tickets excluded).</summary>
    Task<IReadOnlyList<(CodePurpose Purpose, DateTimeOffset SentAt)>> SentSinceAsync(
        string destination,
        DateTimeOffset since,
        CancellationToken cancellationToken);

    /// <summary>Adds the code and retires the other unused codes of the same purpose and destination.</summary>
    Task AddAsync(StoredCode code, CancellationToken cancellationToken);

    Task RecordFailedAttemptAsync(Guid id, CancellationToken cancellationToken);

    Task ConsumeAsync(Guid id, DateTimeOffset at, CancellationToken cancellationToken);
}

/// <summary>Keyed hash of codes and tickets, so a database copy does not reveal them.</summary>
public interface ICodeHasher
{
    string Hash(string code);
}

/// <summary>A stored refresh token (only its hash).</summary>
public sealed record StoredRefreshToken(
    Guid Id,
    Guid UserId,
    Guid FamilyId,
    string Hash,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    Guid? ReplacedById);

public interface IRefreshTokens
{
    Task<StoredRefreshToken?> FindByHashAsync(string hash, CancellationToken cancellationToken);

    Task AddAsync(StoredRefreshToken token, CancellationToken cancellationToken);

    /// <summary>Marks the token used and replaced by its successor; false if it was already used.</summary>
    Task<bool> TryRotateAsync(Guid id, Guid replacedById, DateTimeOffset at, CancellationToken cancellationToken);

    Task RevokeFamilyAsync(Guid familyId, DateTimeOffset at, CancellationToken cancellationToken);

    Task RevokeAllAsync(UserId userId, DateTimeOffset at, CancellationToken cancellationToken);
}

/// <summary>Signs the API's short-lived access tokens.</summary>
public interface IAccessTokenIssuer
{
    (string Token, DateTimeOffset ExpiresAt) Issue(UserId userId, DateTimeOffset now);
}
