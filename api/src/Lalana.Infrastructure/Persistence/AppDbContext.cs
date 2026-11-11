using Lalana.Application.Auth;
using Lalana.Infrastructure.Identity;
using Lalana.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Lalana.Infrastructure.Persistence;

/// <summary>
/// The application's database: every table lives in schema <c>app</c>, including the ASP.NET Core
/// Identity users (no roles: every member can both drive and ride).
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityUserContext<AppUser, Guid>(options)
{
    public const string Schema = "app";

    public DbSet<StoredCode> VerificationCodes => Set<StoredCode>();

    public DbSet<StoredRefreshToken> RefreshTokens => Set<StoredRefreshToken>();

    internal DbSet<ProfileRow> Profiles => Set<ProfileRow>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);
        builder.HasPostgresExtension("postgis");

        builder.Entity<AppUser>(user =>
        {
            user.ToTable("users");
            user.Property(u => u.FirstName).HasMaxLength(50);
            user.Property(u => u.LastName).HasMaxLength(50);
            user.Property(u => u.PhoneNumber).HasMaxLength(16);
            user.HasIndex(u => u.PhoneNumber).IsUnique().HasFilter("phone_number IS NOT NULL");
            user.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique().HasFilter("normalized_email IS NOT NULL");
        });
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
