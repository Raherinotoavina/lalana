using Lalana.Api.Auth;
using Lalana.Api.Endpoints;
using Lalana.Api.Http;
using Lalana.Api.OpenApi;
using Lalana.Application;
using Lalana.Application.Abstractions;
using Lalana.Infrastructure;
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

        services.AddApplication();
        services.AddInfrastructure();

        var authOptions = services.AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.Section)
            .ValidateDataAnnotations();
        if (!OpenApiGeneration.IsRunning)
        {
            authOptions.ValidateOnStart();
        }

        services.AddSingleton<IValidateOptions<AuthOptions>, AuthOptionsValidator>();
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearer>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

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

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) })
            .AllowAnonymous();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
        }

        app.MapAccountEndpoints();
        return app;
    }
}
