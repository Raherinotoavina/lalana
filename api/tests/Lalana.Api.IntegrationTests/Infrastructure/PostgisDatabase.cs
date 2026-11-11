using Lalana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Lalana.Api.IntegrationTests.Infrastructure.PostgisDatabase))]

namespace Lalana.Api.IntegrationTests.Infrastructure;

/// <summary>
/// One PostgreSQL + PostGIS container for the whole test run, migrated like production, and
/// one API instance on top of it.
/// </summary>
public sealed class PostgisDatabase : IAsyncLifetime
{
    /// <summary>Same image as docker compose: api/docker/postgis (multi-architecture).</summary>
    private readonly IFutureDockerImage image = new ImageFromDockerfileBuilder()
        .WithDockerfileDirectory(CommonDirectoryPath.GetGitDirectory(), "api/docker/postgis")
        .WithName("lalana-postgis:tests")
        .WithCleanUp(false)
        .Build();

    private PostgreSqlContainer container = null!;

    public string ConnectionString => container.GetConnectionString();

    public LalanaApiFactory Api { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await image.CreateAsync();
        container = new PostgreSqlBuilder(image).Build();
        await container.StartAsync();
        Api = new LalanaApiFactory(ConnectionString);

        await using var scope = Api.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (Api is not null)
        {
            await Api.DisposeAsync();
        }

        if (container is not null)
        {
            await container.DisposeAsync();
        }

        await image.DisposeAsync();
    }
}
