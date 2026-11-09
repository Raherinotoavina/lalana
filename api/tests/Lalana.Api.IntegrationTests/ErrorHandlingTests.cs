using System.Net;
using System.Text.Json;
using Lalana.Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Lalana.Api.IntegrationTests;

public sealed class ErrorHandlingTests(PostgisDatabase database)
{
    private CancellationToken Cancel => TestContext.Current.CancellationToken;

    private HttpClient AuthenticatedClient() =>
        database.Api.CreateClient().WithBearer(TestTokens.Create());

    [Fact]
    public async Task Unknown_route_returns_problem_details_with_the_correlation_id()
    {
        var response = await AuthenticatedClient().GetAsync("/does-not-exist", Cancel);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var problem = await ReadJson(response);
        problem.GetProperty("status").GetInt32().ShouldBe(404);
        problem.GetProperty("correlationId").GetString()
            .ShouldBe(response.Headers.GetValues("X-Correlation-Id").Single());
    }

    [Fact]
    public async Task Unhandled_exception_returns_a_500_without_internal_details()
    {
        var response = await AuthenticatedClient().GetAsync(LalanaApiFactory.ThrowPath, Cancel);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadAsStringAsync(Cancel)).ShouldNotContain("secret detail");
    }

    [Fact]
    public async Task Keeps_a_safe_correlation_id_sent_by_the_caller()
    {
        var client = database.Api.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "mobile-1234");

        var response = await client.GetAsync("/health", Cancel);

        response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe("mobile-1234");
    }

    [Fact]
    public async Task Replaces_an_unsafe_correlation_id()
    {
        var client = database.Api.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-Id", "bad id with spaces and a very long value that keeps going");

        var response = await client.GetAsync("/health", Cancel);

        response.Headers.GetValues("X-Correlation-Id").Single().ShouldMatch("^[0-9a-f]{32}$");
    }

    private async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancel)).RootElement;
}
