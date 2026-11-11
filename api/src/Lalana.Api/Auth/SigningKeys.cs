using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lalana.Api.Auth;

/// <summary>The ES256 key that signs and verifies access tokens, shared by issuing and validation.</summary>
internal sealed partial class SigningKeys
{
    public SigningKeys(IOptions<AuthOptions> options, ILogger<SigningKeys> logger)
    {
        var pem = options.Value.SigningKeyPem;
        ECDsa key;
        if (string.IsNullOrWhiteSpace(pem))
        {
            // Only reachable in Development (AuthOptionsValidator): tokens die with the process.
            key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            LogEphemeralKey(logger);
        }
        else if (!TryImport(pem, out key))
        {
            throw new InvalidOperationException("Auth:SigningKeyPem is not an ECDSA P-256 private key.");
        }

        Key = new ECDsaSecurityKey(key);
        Credentials = new SigningCredentials(Key, SecurityAlgorithms.EcdsaSha256);
    }

    public ECDsaSecurityKey Key { get; }

    public SigningCredentials Credentials { get; }

    public static bool TryImport(string pem, out ECDsa key)
    {
        key = ECDsa.Create();
        try
        {
            key.ImportFromPem(pem);
            var parameters = key.ExportParameters(includePrivateParameters: true);
            return parameters.Curve.Oid.Value == ECCurve.NamedCurves.nistP256.Oid.Value && parameters.D is not null;
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException)
        {
            return false;
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Auth:SigningKeyPem is empty: using a throwaway signing key; access tokens stop working on restart.")]
    private static partial void LogEphemeralKey(ILogger logger);
}
