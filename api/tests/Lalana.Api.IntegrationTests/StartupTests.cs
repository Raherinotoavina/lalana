using System.Net;
using Lalana.Api.IntegrationTests.Infrastructure;
using System.Security.Cryptography;
using Lalana.Api.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Shouldly;

namespace Lalana.Api.IntegrationTests;

public sealed class StartupTests(PostgisDatabase database)
{
    private CancellationToken Cancel => TestContext.Current.CancellationToken;

    // When startup fails, Program's RunAsync disposes the host on its own thread while
    // WebApplicationFactory may still be reading it, so the exception that reaches the test
    // is sometimes an ObjectDisposedException. These tests only check that startup fails;
    // the messages are checked on the validators below.
    [Theory]
    [InlineData("Production", false, null)]
    [InlineData("Production", true, null)]
    [InlineData("Staging", true, MessagingOptions.Development)]
    public async Task Refuses_to_start_with_missing_secrets_or_provider(string environment, bool withKeys, string? provider)
    {
        await using var api = new LalanaApiFactory(database.ConnectionString, environment, withSigningKey: withKeys, messagingProvider: provider);

        Should.Throw<Exception>(() => api.CreateClient());
    }

    [Theory]
    [MemberData(nameof(MalformedSecrets))]
    public async Task Refuses_to_start_with_malformed_auth_secrets(string signingKey, string codeHashKey)
    {
        await using var api = new LalanaApiFactory(database.ConnectionString, "Development")
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Auth:SigningKeyPem", signingKey);
                builder.UseSetting("Auth:CodeHashKey", codeHashKey);
            });

        Should.Throw<Exception>(() => api.CreateClient());
    }

    public static TheoryData<string, string> MalformedSecrets()
    {
        using var p384 = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using var p256 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new()
        {
            { "not a key", TestTokens.CodeHashKey },
            { p384.ExportPkcs8PrivateKeyPem(), TestTokens.CodeHashKey },
            { p256.ExportSubjectPublicKeyInfoPem(), TestTokens.CodeHashKey },
            { TestTokens.PrivateKeyPem, "not base64!" },
            { TestTokens.PrivateKeyPem, "dGVzdA==" },
        };
    }

    [Fact]
    public void Auth_validator_requires_both_secrets_outside_development()
    {
        var result = new AuthOptionsValidator(Environment("Production")).Validate(null, new AuthOptions());

        result.Failed.ShouldBeTrue();
        var message = result.FailureMessage ?? string.Empty;
        message.ShouldContain("Auth:SigningKeyPem is required");
        message.ShouldContain("Auth:CodeHashKey is required");
        new AuthOptionsValidator(Environment("Development")).Validate(null, new AuthOptions()).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Auth_validator_refuses_malformed_secrets()
    {
        using var p384 = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using var p256 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var validator = new AuthOptionsValidator(Environment("Development"));
        string Failure(string? signingKey, string? codeHashKey) =>
            validator.Validate(null, new AuthOptions { SigningKeyPem = signingKey, CodeHashKey = codeHashKey }).FailureMessage ?? string.Empty;

        Failure("not a key", null).ShouldContain("is not an ECDSA P-256 private key");
        Failure(p384.ExportPkcs8PrivateKeyPem(), null).ShouldContain("is not an ECDSA P-256 private key");
        Failure(p256.ExportSubjectPublicKeyInfoPem(), null).ShouldContain("is not an ECDSA P-256 private key");
        Failure(null, "not base64!").ShouldContain("must be base64 of at least 32 bytes");
        Failure(null, "dGVzdA==").ShouldContain("must be base64 of at least 32 bytes");
        validator.Validate(null, new AuthOptions { SigningKeyPem = TestTokens.PrivateKeyPem, CodeHashKey = TestTokens.CodeHashKey })
            .Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Messaging_validator_needs_a_real_provider_outside_development()
    {
        string? Failure(string environment, string? provider) =>
            new MessagingOptionsValidator(Environment(environment)).Validate(null, new MessagingOptions { Provider = provider }).FailureMessage;

        (Failure("Production", null) ?? string.Empty).ShouldContain("Messaging:Provider is not set");
        (Failure("Staging", MessagingOptions.Development) ?? string.Empty).ShouldContain("refused outside Development");
        Failure("Testing", null).ShouldBeNull();
        Failure("Development", MessagingOptions.Development).ShouldBeNull();
    }

    private static HostingEnvironment Environment(string name) => new() { EnvironmentName = name };

    [Fact]
    public async Task Starts_in_development_without_keys_and_accepts_no_foreign_token()
    {
        await using var api = new LalanaApiFactory(database.ConnectionString, "Development", withSigningKey: false);
        var client = api.CreateClient().WithBearer(TestTokens.Create());

        var response = await client.GetAsync("/me", Cancel);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Serves_the_openapi_document_in_development_only()
    {
        await using var development = new LalanaApiFactory(database.ConnectionString, "Development");

        var document = await development.CreateClient().GetAsync("/openapi/v1.json", Cancel);
        var hidden = await database.Api.CreateClient().WithBearer(TestTokens.Create()).GetAsync("/openapi/v1.json", Cancel);

        document.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await document.Content.ReadAsStringAsync(Cancel)).ShouldContain("\"/me\"");
        hidden.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
