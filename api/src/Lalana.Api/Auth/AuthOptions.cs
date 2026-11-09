using System.ComponentModel.DataAnnotations;

namespace Lalana.Api.Auth;

/// <summary>Validation settings for the API's own access tokens (section <c>Auth</c>).</summary>
public sealed class AuthOptions
{
    public const string Section = "Auth";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// PEM of the ECDSA P-256 public key that verifies access tokens (ES256). Required outside
    /// Development; in Development a throwaway key is generated when it is empty.
    /// </summary>
    public string? SigningPublicKeyPem { get; set; }
}
