using Lalana.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lalana.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the database. The connection string is read when the first context is created,
    /// so tools that only build the app (OpenAPI generation) need no database.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            var connectionString = provider
                .GetRequiredService<IConfiguration>()
                .GetConnectionString(DatabaseOptions.ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{DatabaseOptions.ConnectionStringName}' is missing. "
                        + "Set ConnectionStrings__Lalana (see api/.env.example).");
            }

            options.UseLalanaPostgres(connectionString);
        });
        return services;
    }
}
