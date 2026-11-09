using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lalana.Api.IntegrationTests.Infrastructure;

/// <summary>The real API, in memory, against the test database.</summary>
public sealed class LalanaApiFactory(
    string connectionString,
    string environment = "Testing",
    bool withSigningKey = true,
    bool migrateOnStartup = false) : WebApplicationFactory<Program>
{
    public const string ThrowPath = "/__tests/throw";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:Lalana", connectionString);
        builder.UseSetting("Auth:Issuer", TestTokens.Issuer);
        builder.UseSetting("Auth:Audience", TestTokens.Audience);
        builder.UseSetting("Auth:SigningPublicKeyPem", withSigningKey ? TestTokens.PublicKeyPem : string.Empty);
        builder.UseSetting("Database:MigrateOnStartup", migrateOnStartup ? "true" : "false");
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, ThrowingRouteStartupFilter>());
    }

    /// <summary>Adds a route that throws, after the app's own pipeline, to test the error handler.</summary>
    private sealed class ThrowingRouteStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(
            Action<IApplicationBuilder> next) =>
            app =>
            {
                next(app);
                app.Use((context, nextMiddleware) =>
                    context.Request.Path == ThrowPath
                        ? throw new InvalidOperationException("Test failure with secret detail.")
                        : nextMiddleware(context));
            };
    }
}
