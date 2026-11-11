using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Lalana.Api.IntegrationTests.Infrastructure;

/// <summary>Mints ES256 access tokens for the tests, signed by a key known only to the tests.</summary>
internal static class TestTokens
{
    public const string Issuer = "https://api.lalana.test";
    public const string Audience = "lalana-tests";

    private static readonly ECDsa SigningKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    /// <summary>The key the API under test signs with (and verifies), so tests can also mint tokens.</summary>
    public static string PrivateKeyPem { get; } = SigningKey.ExportPkcs8PrivateKeyPem();

    /// <summary>Base64 HMAC key for one-time codes.</summary>
    public static string CodeHashKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static string Create(
        string? subject = null,
        string issuer = Issuer,
        string audience = Audience,
        TimeSpan? expiresIn = null,
        ECDsa? key = null)
    {
        var now = DateTime.UtcNow;
        var expires = now + (expiresIn ?? TimeSpan.FromMinutes(5));
        var issuedAt = expires < now ? expires.AddMinutes(-10) : now;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expires,
            Claims = new Dictionary<string, object> { ["sub"] = subject ?? Guid.NewGuid().ToString() },
            SigningCredentials = new SigningCredentials(
                new ECDsaSecurityKey(key ?? SigningKey),
                SecurityAlgorithms.EcdsaSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
