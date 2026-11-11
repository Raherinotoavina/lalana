using Lalana.Application.Auth;
using Lalana.Application.Messaging;
using Lalana.Application.Profiles;
using Lalana.Domain.Identity;
using Lalana.Infrastructure.Identity;
using Lalana.Infrastructure.Messaging;
using Lalana.Infrastructure.Persistence;
using Lalana.Infrastructure.Persistence.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lalana.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the database, the Identity stores and the auth stores. The connection string is
    /// read when the first context is created, so tools that only build the app (OpenAPI
    /// generation) need no database.
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

        services
            .AddIdentityCore<AppUser>(options =>
            {
                // E-mail is optional (phone-only accounts); uniqueness is the database's unique index.
                options.User.RequireUniqueEmail = false;
                options.Password.RequiredLength = PasswordRules.MinLength;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IAccounts, IdentityAccounts>();
        services.AddScoped<IVerificationCodes, EfVerificationCodes>();
        services.AddScoped<IRefreshTokens, EfRefreshTokens>();
        services.AddScoped<IProfiles, EfProfiles>();
        return services;
    }

    /// <summary>Keeps SMS and e-mail in memory instead of sending them (Development and tests only).</summary>
    public static IServiceCollection AddDevelopmentMessaging(this IServiceCollection services)
    {
        services.AddSingleton<DevelopmentMessages>();
        services.AddSingleton<ISmsSender>(provider => provider.GetRequiredService<DevelopmentMessages>());
        services.AddSingleton<IEmailSender>(provider => provider.GetRequiredService<DevelopmentMessages>());
        return services;
    }
}
