using System.Security.Cryptography;
using Lalana.Domain.Identity;

namespace Lalana.Application.Auth;

/// <summary>Tokens returned when a member signs in.</summary>
public sealed record AuthSession(
    UserId UserId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

/// <summary>
/// Starts, refreshes and ends sessions. Refresh tokens are random, stored hashed, rotated on use;
/// reusing an old one revokes its whole family (the token was probably stolen).
/// </summary>
public sealed class Sessions(
    IRefreshTokens refreshTokens,
    IAccessTokenIssuer accessTokens,
    AuthSettings settings,
    TimeProvider clock)
{
    public async Task<AuthSession> StartAsync(UserId userId, CancellationToken cancellationToken) =>
        (await IssueAsync(userId, Guid.CreateVersion7(), null, cancellationToken)).Value!;

    public async Task<AuthResult<AuthSession>> RefreshAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return AuthError.InvalidRefreshToken;
        }

        var stored = await refreshTokens.FindByHashAsync(Hash(refreshToken), cancellationToken);
        var now = clock.GetUtcNow();
        if (stored is null)
        {
            return AuthError.InvalidRefreshToken;
        }

        if (stored.RevokedAt is not null || stored.ReplacedById is not null)
        {
            await refreshTokens.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            return AuthError.InvalidRefreshToken;
        }

        if (stored.ExpiresAt <= now)
        {
            return AuthError.InvalidRefreshToken;
        }

        return await IssueAsync(UserId.From(stored.UserId), stored.FamilyId, stored, cancellationToken);
    }

    /// <summary>Ends the session of this refresh token (its family); unknown tokens are ignored.</summary>
    public async Task EndAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        if (await refreshTokens.FindByHashAsync(Hash(refreshToken), cancellationToken) is { } stored)
        {
            await refreshTokens.RevokeFamilyAsync(stored.FamilyId, clock.GetUtcNow(), cancellationToken);
        }
    }

    public Task EndAllAsync(UserId userId, CancellationToken cancellationToken) =>
        refreshTokens.RevokeAllAsync(userId, clock.GetUtcNow(), cancellationToken);

    internal static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private async Task<AuthResult<AuthSession>> IssueAsync(
        UserId userId,
        Guid familyId,
        StoredRefreshToken? previous,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var next = new StoredRefreshToken(
            Guid.CreateVersion7(),
            userId.Value,
            familyId,
            Hash(refresh),
            now,
            now + settings.RefreshTokenLifetime,
            null,
            null);

        if (previous is not null && !await refreshTokens.TryRotateAsync(previous.Id, next.Id, now, cancellationToken))
        {
            // Another request used this token first: treat it as reuse.
            await refreshTokens.RevokeFamilyAsync(familyId, now, cancellationToken);
            return AuthError.InvalidRefreshToken;
        }

        await refreshTokens.AddAsync(next, cancellationToken);
        var (access, accessExpiresAt) = accessTokens.Issue(userId, now);
        return new AuthSession(userId, access, accessExpiresAt, refresh, next.ExpiresAt);
    }
}
