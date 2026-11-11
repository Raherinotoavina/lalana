using Lalana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Lalana.Api.IntegrationTests.Infrastructure;

namespace Lalana.Api.IntegrationTests;

public sealed class DatabaseTests(PostgisDatabase database)
{
    [Fact]
    public async Task Migrations_create_the_app_schema_and_enable_postgis()
    {
        await using var scope = database.Api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cancel = TestContext.Current.CancellationToken;

        var schemas = await db.Database
            .SqlQueryRaw<string>("SELECT schema_name AS \"Value\" FROM information_schema.schemata WHERE schema_name = 'app'")
            .ToListAsync(cancel);
        var postgis = await db.Database
            .SqlQueryRaw<string>("SELECT extname AS \"Value\" FROM pg_extension WHERE extname = 'postgis'")
            .ToListAsync(cancel);
        var pending = await db.Database.GetPendingMigrationsAsync(cancel);

        schemas.ShouldBe(["app"]);
        postgis.ShouldBe(["postgis"]);
        pending.ShouldBeEmpty();
    }

    [Fact]
    public async Task Migrate_on_startup_is_idempotent()
    {
        await using var api = new LalanaApiFactory(database.ConnectionString, migrateOnStartup: true);

        var response = await api.CreateClient().GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact]
    public void A_missing_connection_string_fails_with_a_clear_message()
    {
        using var api = new LalanaApiFactory(connectionString: string.Empty);
        using var scope = api.Services.CreateScope();

        var error = Should.Throw<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<AppDbContext>());

        error.Message.ShouldContain("Connection string 'Lalana' is missing");
    }
}
