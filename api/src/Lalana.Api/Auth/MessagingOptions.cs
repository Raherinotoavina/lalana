using Microsoft.Extensions.Options;

namespace Lalana.Api.Auth;

/// <summary>Which SMS and e-mail providers send codes (section <c>Messaging</c>).</summary>
public sealed class MessagingOptions
{
    public const string Section = "Messaging";

    /// <summary>The in-memory stand-in, allowed in Development and Testing only.</summary>
    public const string Development = "Development";

    /// <summary>Defaults to <see cref="Development"/> in Development and Testing.</summary>
    public string? Provider { get; set; }
}

internal sealed class MessagingOptionsValidator(IHostEnvironment environment) : IValidateOptions<MessagingOptions>
{
    public static bool AllowsDevelopmentProvider(IHostEnvironment environment) =>
        environment.IsDevelopment() || environment.IsEnvironment("Testing");

    public ValidateOptionsResult Validate(string? name, MessagingOptions options)
    {
        // An empty value (Compose passes MESSAGING_PROVIDER= as "") means "not set".
        var provider = string.IsNullOrWhiteSpace(options.Provider) ? null : options.Provider;
        provider ??= (AllowsDevelopmentProvider(environment) ? MessagingOptions.Development : null);
        return provider switch
        {
            MessagingOptions.Development when AllowsDevelopmentProvider(environment) => ValidateOptionsResult.Success,
            MessagingOptions.Development => ValidateOptionsResult.Fail(
                "Messaging:Provider 'Development' only keeps codes in memory; it is refused outside Development."),
            _ => ValidateOptionsResult.Fail(
                "Messaging:Provider is not set: no SMS or e-mail provider is available yet (Phase 03 chooses them)."),
        };
    }
}
