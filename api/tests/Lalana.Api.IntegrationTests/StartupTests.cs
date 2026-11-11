using System.Net;
using Lalana.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Shouldly;

namespace Lalana.Api.IntegrationTests;

public sealed class StartupTests(PostgisDatabase database)
{
    private CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Refuses_to_start_outside_development_without_a_signing_key()
    {
        await using var api = new LalanaApiFactory(database.ConnectionString, "Production", withSigningKey: false);

        var error = Should.Throw<Exception>(() => api.CreateClient());

        error.ToString().ShouldContain("Auth:SigningKeyPem is required");
        error.ToString().ShouldContain("Auth:CodeHashKey is required");
    }

    [Fact]
    public async Task Refuses_to_start_outside_development_without_a_messaging_provider()
    {
        await using var api = new LalanaApiFactory(database.ConnectionString, "Production");

        var error = Should.Throw<Exception>(() => api.CreateClient());

        error.ToString().ShouldContain("Messaging:Provider is not set");
    }

    [Fact]
    public async Task Refuses_the_in_memory_messaging_provider_outside_development()
    {
        await using var api = new LalanaApiFactory(database.ConnectionString, "Staging", messagingProvider: "Development");

        var error = Should.Throw<Exception>(() => api.CreateClient());

        error.ToString().ShouldContain("refused outside Development");
    }

    [Theory]
    [InlineData("not a key", "dGVzdA==", "is not an ECDSA P-256 private key")]
    [InlineData(null, "not base64!", "must be base64 of at least 32 bytes")]
    [InlineData(null, "dGVzdA==", "must be base64 of at least 32 bytes")]
    public async Task Refuses_malformed_auth_secrets(string? signingKey, string codeHashKey, string message)
    {
        await using var api = new LalanaApiFactory(database.ConnectionString, "Development")
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Auth:SigningKeyPem", signingKey ?? TestTokens.PrivateKeyPem);
                builder.UseSetting("Auth:CodeHashKey", codeHashKey);
            });

        var error = Should.Throw<Exception>(() => api.CreateClient());

        error.ToString().ShouldContain(message);
    }

    [Fact]
    public async Task Refuses_a_public_key_or_another_curve_as_signing_key()
    {
        using var p384 = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP384);
        using var p256 = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
        foreach (var pem in new[] { p384.ExportPkcs8PrivateKeyPem(), p256.ExportSubjectPublicKeyInfoPem() })
        {
            await using var api = new LalanaApiFactory(database.ConnectionString, "Development")
                .WithWebHostBuilder(builder => builder.UseSetting("Auth:SigningKeyPem", pem));

            Should.Throw<Exception>(() => api.CreateClient()).ToString().ShouldContain("is not an ECDSA P-256 private key");
        }
    }

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
