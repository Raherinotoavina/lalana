using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Lalana.Api.Http;

/// <summary>Limits of the anonymous auth endpoints (section <c>RateLimiting</c>).</summary>
public sealed class RateLimitingOptions
{
    public const string Section = "RateLimiting";

    /// <summary>Requests to <c>/auth</c> per minute, per IP address and per device.</summary>
    public int AuthPermitsPerMinute { get; set; } = 30;
}

/// <summary>
/// Caps calls to <c>/auth</c> per client IP and per device (<c>X-Device-Id</c> header), on top of the
/// per-phone and per-e-mail code limits of the application. Other paths are not limited here.
/// </summary>
internal static class AuthRateLimiting
{
    public const string DeviceHeader = "X-Device-Id";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitingOptions.Section).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
        services.AddRateLimiter(limiter =>
        {
            limiter.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                ByKey(options, context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
                ByKey(options, context => context.Request.Headers[DeviceHeader].FirstOrDefault()));
            limiter.OnRejected = (rejected, cancellationToken) =>
            {
                var response = rejected.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    response.Headers.RetryAfter = ((long)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                return new ValueTask(rejected.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(
                    new ProblemDetailsContext
                    {
                        HttpContext = rejected.HttpContext,
                        ProblemDetails =
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Too many requests; wait before trying again.",
                            Extensions = { ["code"] = "too_many_requests" },
                        },
                    }).AsTask());
            };
        });
        return services;
    }

    private static PartitionedRateLimiter<HttpContext> ByKey(RateLimitingOptions options, Func<HttpContext, string?> key) =>
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var partition = context.Request.Path.StartsWithSegments("/auth") ? key(context) : null;
            return string.IsNullOrEmpty(partition)
                ? RateLimitPartition.GetNoLimiter(string.Empty)
                : RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.AuthPermitsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                });
        });
}
