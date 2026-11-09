using System.Net;
using Lalana.Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Lalana.Api.IntegrationTests;

public sealed class HealthTests(PostgisDatabase database)
{
    [Fact]
    public async Task Liveness_answers_without_a_token()
    {
        var response = await database.Api.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe("Healthy");
    }

    [Fact]
    public async Task Readiness_reports_the_database_as_reachable()
    {
        var response = await database.Api.CreateClient().GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Readiness_fails_when_the_database_is_down()
    {
        await using var api = new LalanaApiFactory("Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2");

        var response = await api.CreateClient().GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }
}
