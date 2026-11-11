using System.ComponentModel.DataAnnotations;

namespace Lalana.Api.Auth;

/// <summary>Signing and hashing secrets of the API's own tokens and codes (section <c>Auth</c>).</summary>
public sealed class AuthOptions
{
    public const string Section = "Auth";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// PEM of the ECDSA P-256 private key that signs access tokens (ES256); its public part
    /// verifies them. Required outside Development, where a throwaway key is generated when empty.
    /// </summary>
    public string? SigningKeyPem { get; set; }

    /// <summary>
    /// Base64 key (32 bytes or more) of the HMAC that hashes one-time codes and reset tickets.
    /// Required outside Development, where a throwaway key is generated when empty.
    /// </summary>
    public string? CodeHashKey { get; set; }

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
}
