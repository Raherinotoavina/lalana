using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Lalana.Api.Endpoints;
using Lalana.Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Lalana.Api.IntegrationTests;

public sealed class MeTests(PostgisDatabase database)
{
    private CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Returns_the_callers_account_with_a_valid_token()
    {
        var auth = new AuthApi(database.Api);
        var email = AuthApi.NewEmail();
        var (session, _, e164) = await auth.MemberAsync(email);

        var me = await auth.AuthorizedClient(session).GetFromJsonAsync<MeResponse>("/me", Cancel);

        me.ShouldBe(new MeResponse(session.UserId, "Sitraka", "Rabe", e164, true, email, false, false));
    }

    [Fact]
    public async Task Rejects_a_valid_token_for_an_account_that_does_not_exist()
    {
        var client = database.Api.CreateClient().WithBearer(TestTokens.Create(Guid.NewGuid().ToString()));

        var response = await client.GetAsync("/me", Cancel);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Rejects_a_request_without_a_token_with_problem_details()
    {
        var response = await database.Api.CreateClient().GetAsync("/me", Cancel);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().ShouldStartWith("Bearer");
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    public static TheoryData<string, string> InvalidTokens => new()
    {
        { "expired", TestTokens.Create(expiresIn: TimeSpan.FromMinutes(-5)) },
        { "wrong issuer", TestTokens.Create(issuer: "https://evil.example") },
        { "wrong audience", TestTokens.Create(audience: "another-app") },
        { "wrong key", TestTokens.Create(key: ECDsa.Create(ECCurve.NamedCurves.nistP256)) },
        { "subject is not a user id", TestTokens.Create(subject: "not-a-guid") },
        { "not a token", "abc.def.ghi" },
    };

    [Theory]
    [MemberData(nameof(InvalidTokens))]
    public async Task Rejects_an_invalid_token(string reason, string token)
    {
        var client = database.Api.CreateClient().WithBearer(token);

        var response = await client.GetAsync("/me", Cancel);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, reason);
    }
}
