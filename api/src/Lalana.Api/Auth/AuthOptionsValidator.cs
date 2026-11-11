using Lalana.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace Lalana.Api.Auth;

internal sealed class AuthOptionsValidator(IHostEnvironment environment) : IValidateOptions<AuthOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.SigningKeyPem))
        {
            if (!environment.IsDevelopment())
            {
                failures.Add("Auth:SigningKeyPem is required outside Development (see api/.env.example).");
            }
        }
        else if (!SigningKeys.TryImport(options.SigningKeyPem, out _))
        {
            failures.Add("Auth:SigningKeyPem is not an ECDSA P-256 private key in PEM form.");
        }

        if (string.IsNullOrWhiteSpace(options.CodeHashKey))
        {
            if (!environment.IsDevelopment())
            {
                failures.Add("Auth:CodeHashKey is required outside Development (see api/.env.example).");
            }
        }
        else if (!TryDecodeKey(options.CodeHashKey, out _))
        {
            failures.Add($"Auth:CodeHashKey must be base64 of at least {HmacCodeHasher.MinKeyLength} bytes.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    public static bool TryDecodeKey(string base64, out byte[] key)
    {
        key = [];
        try
        {
            key = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return false;
        }

        return key.Length >= HmacCodeHasher.MinKeyLength;
    }
}
