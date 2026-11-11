using Microsoft.Extensions.Options;

namespace Lalana.Api.Auth;

internal sealed class AuthOptionsValidator(IHostEnvironment environment) : IValidateOptions<AuthOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SigningPublicKeyPem) && !environment.IsDevelopment())
        {
            return ValidateOptionsResult.Fail(
                "Auth:SigningPublicKeyPem is required outside Development (see api/.env.example).");
        }

        return ValidateOptionsResult.Success;
    }
}
