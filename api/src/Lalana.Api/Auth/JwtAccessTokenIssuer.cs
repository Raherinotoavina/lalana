using Lalana.Application.Auth;
using Lalana.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Lalana.Api.Auth;

/// <summary>Signs ES256 access tokens whose <c>sub</c> is the member's id.</summary>
internal sealed class JwtAccessTokenIssuer(IOptions<AuthOptions> options, SigningKeys keys) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler handler = new();

    public (string Token, DateTimeOffset ExpiresAt) Issue(UserId userId, DateTimeOffset now)
    {
        var settings = options.Value;
        var expiresAt = now + settings.AccessTokenLifetime;
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = userId.ToString(),
                ["jti"] = Guid.NewGuid().ToString("N"),
            },
            SigningCredentials = keys.Credentials,
        });
        return (token, expiresAt);
    }
}
