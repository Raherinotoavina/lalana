using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lalana.Api.Auth;

/// <summary>Validates the API's own ES256 access tokens: signature, issuer, audience, lifetime.</summary>
internal sealed class ConfigureJwtBearer(
    IOptions<AuthOptions> authOptions,
    SigningKeys signingKeys,
    IHostEnvironment environment) : IConfigureNamedOptions<JwtBearerOptions>
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
            IssuerSigningKey = signingKeys.Key,
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
}
