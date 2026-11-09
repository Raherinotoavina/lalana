using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lalana.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef migrations add</c> only. Creating a migration does not connect to a
/// database, so no real connection string is needed here.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Design-time tooling, not runtime code.")]
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseLalanaPostgres("Host=localhost;Database=lalana_design");
        return new AppDbContext(builder.Options);
    }
}
