using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Lalana.Infrastructure.Persistence;

public static class DatabaseOptions
{
    public const string ConnectionStringName = "Lalana";
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    /// <summary>Shared Npgsql settings for the app and the design-time factory.</summary>
    public static DbContextOptionsBuilder UseLalanaPostgres(
        this DbContextOptionsBuilder builder,
        string connectionString) =>
        builder
            .UseNpgsql(connectionString, ConfigureNpgsql)
            .UseSnakeCaseNamingConvention();

    private static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql)
    {
        npgsql.UseNetTopologySuite();
        npgsql.MigrationsHistoryTable(MigrationsHistoryTable, AppDbContext.Schema);
    }
}
