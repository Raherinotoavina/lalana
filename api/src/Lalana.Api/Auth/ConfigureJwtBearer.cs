using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lalana.Api.Auth;

/// <summary>Validates the API's own ES256 access tokens: signature, issuer, audience, lifetime.</summary>
internal sealed partial class ConfigureJwtBearer(
    IOptions<AuthOptions> authOptions,
    IHostEnvironment environment,
    ILogger<ConfigureJwtBearer> logger) : IConfigureNamedOptions<JwtBearerOptions>
{
    public static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        var settings = authOptions.Value;
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = !environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = CreateSigningKey(settings.SigningPublicKeyPem),
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = ClockSkew,
            NameClaimType = "sub",
        };
    }

    public void Configure(JwtBearerOptions options) =>
        Configure(JwtBearerDefaults.AuthenticationScheme, options);

    private ECDsaSecurityKey CreateSigningKey(string? publicKeyPem)
    {
        var key = ECDsa.Create();
        if (string.IsNullOrWhiteSpace(publicKeyPem))
        {
            // Only reachable in Development (AuthOptionsValidator): no token can be valid.
            key.GenerateKey(ECCurve.NamedCurves.nistP256);
            LogEphemeralKey(logger);
        }
        else
        {
            key.ImportFromPem(publicKeyPem);
        }

        return new ECDsaSecurityKey(key);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Auth:SigningPublicKeyPem is empty: using a throwaway key, no access token will be accepted.")]
    private static partial void LogEphemeralKey(ILogger logger);
}
