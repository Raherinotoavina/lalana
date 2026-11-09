using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lalana.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    /// <summary>
    /// Applies pending migrations. Used by the local Docker Compose stack
    /// (<c>Database:MigrateOnStartup</c>); deployments run migrations as their own step.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(cancellationToken);
    }
}
