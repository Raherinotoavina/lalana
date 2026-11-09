using System.Diagnostics;

namespace Lalana.Api.Http;

/// <summary>
/// One log line per request: method, path, status and duration. The query string, headers and
/// bodies are never logged, so no token, code or phone number reaches the logs.
/// </summary>
internal static partial class RequestLogging
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Lalana.Request");
            var started = Stopwatch.GetTimestamp();
            try
            {
                await next(context);
            }
            finally
            {
                LogRequest(
                    logger,
                    context.Request.Method,
                    context.Request.Path.Value ?? "/",
                    context.Response.StatusCode,
                    Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            }
        });

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{Method} {Path} responded {StatusCode} in {ElapsedMs:0.0} ms")]
    private static partial void LogRequest(ILogger logger, string method, string path, int statusCode, double elapsedMs);
}
