using Lalana.Api.Auth;
using Lalana.Api.Endpoints;
using Lalana.Api.Http;
using Lalana.Api.OpenApi;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lalana.Application;
using Lalana.Application.Abstractions;
using Lalana.Application.Auth;
using Lalana.Infrastructure;
using Lalana.Infrastructure.Identity;
using Lalana.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Lalana.Api.Hosting;

internal static class ApiSetup
{
    public const string ReadyTag = "ready";

    public static WebApplicationBuilder AddLalanaApi(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        builder.Logging.ClearProviders();
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddSimpleConsole(options => options.IncludeScopes = true);
        }
        else
        {
            builder.Logging.AddJsonConsole(options =>
            {
                options.IncludeScopes = true;
                options.UseUtcTimestamp = true;
            });
        }

        services.AddSingleton(builder.Configuration.GetSection(AuthOptions.Section).Get<AuthSettings>() ?? new AuthSettings());
        services.AddApplication();
        services.AddInfrastructure();
        services.AddDevelopmentMessaging();

        var authOptions = services.AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.Section)
            .ValidateDataAnnotations();
        var messagingOptions = services.AddOptions<MessagingOptions>().BindConfiguration(MessagingOptions.Section);
        if (!OpenApiGeneration.IsRunning)
        {
            authOptions.ValidateOnStart();
            messagingOptions.ValidateOnStart();
        }

        services.AddSingleton<IValidateOptions<AuthOptions>, AuthOptionsValidator>();
        services.AddSingleton<IValidateOptions<MessagingOptions>, MessagingOptionsValidator>();
        services.AddSingleton<SigningKeys>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddSingleton<ICodeHasher>(provider => new HmacCodeHasher(CodeHashKey(provider.GetRequiredService<IOptions<AuthOptions>>().Value)));
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearer>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        services.AddAuthRateLimiting(builder.Configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Extensions["correlationId"] = CorrelationId.Get(context.HttpContext));

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>(tags: [ReadyTag]);

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer(OpenApiTransformers.DescribeDocument);
            options.AddOperationTransformer(OpenApiTransformers.RequireBearer);
        });

        return builder;
    }

    public static WebApplication UseLalanaApi(this WebApplication app)
    {
        app.UseCorrelationId();
        app.UseRequestLogging();
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) })
            .AllowAnonymous();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
            app.MapDevelopmentEndpoints();
        }

        app.MapAuthEndpoints();
        app.MapAccountEndpoints();
        return app;
    }

    /// <summary>The configured code hash key, or a throwaway one in Development (see AuthOptionsValidator).</summary>
    private static byte[] CodeHashKey(AuthOptions options) =>
        !string.IsNullOrWhiteSpace(options.CodeHashKey) && AuthOptionsValidator.TryDecodeKey(options.CodeHashKey, out var key)
            ? key
            : System.Security.Cryptography.RandomNumberGenerator.GetBytes(HmacCodeHasher.MinKeyLength);
}
