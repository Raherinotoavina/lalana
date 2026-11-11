namespace Lalana.Application.Auth;

/// <summary>Why an auth use case refused; the API maps each to a problem with a stable code.</summary>
public enum AuthError
{
    IdentifierRequired,
    InvalidPhone,
    InvalidEmail,
    NameRequired,
    WeakPassword,
    PhoneTaken,
    EmailTaken,
    InvalidCredentials,
    AccountLocked,
    AccountNotVerified,
    AccountNotFound,
    AlreadyVerified,
    CodeInvalid,
    CodeExpired,
    CodeAttemptsExceeded,
    CodeRateLimited,
    InvalidRefreshToken,
    InvalidResetTicket,
}

/// <summary>An auth failure, with when to retry for rate limits.</summary>
public sealed record AuthFailure(AuthError Error, TimeSpan? RetryAfter = null);

/// <summary>Either a value or a failure.</summary>
public sealed record AuthResult<T>(T? Value, AuthFailure? Failure)
{
    public bool Succeeded => Failure is null;

    public static implicit operator AuthResult<T>(T value) => new(value, null);

    public static implicit operator AuthResult<T>(AuthFailure failure) => new(default, failure);

    public static implicit operator AuthResult<T>(AuthError error) => new(default, new AuthFailure(error));
}
