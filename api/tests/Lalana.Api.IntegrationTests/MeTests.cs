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
    public async Task Returns_the_callers_id_with_a_valid_token()
    {
        var userId = Guid.NewGuid();
        var client = database.Api.CreateClient().WithBearer(TestTokens.Create(userId.ToString()));

        var me = await client.GetFromJsonAsync<MeResponse>("/me", Cancel);

        me.ShouldBe(new MeResponse(userId));
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
