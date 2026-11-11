using System.Net;
using Lalana.Api.IntegrationTests.Infrastructure;
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

        error.ToString().ShouldContain("Auth:SigningPublicKeyPem is required");
    }

    [Fact]
    public async Task Starts_in_development_without_a_key_but_accepts_no_token()
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
