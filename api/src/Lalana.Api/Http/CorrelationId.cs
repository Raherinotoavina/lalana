using System.Text.RegularExpressions;

namespace Lalana.Api.Http;

/// <summary>
/// Gives every request a correlation id: the caller's <c>X-Correlation-Id</c> when it is safe,
/// otherwise a new one. It is echoed in the response, added to log scopes and problem details.
/// </summary>
internal static partial class CorrelationId
{
    public const string Header = "X-Correlation-Id";
    private const string ItemKey = "Lalana.CorrelationId";

    public static string Get(HttpContext context) =>
        context.Items.TryGetValue(ItemKey, out var value) && value is string id ? id : context.TraceIdentifier;

    public static string Resolve(string? candidate) =>
        candidate is not null && SafeId().IsMatch(candidate) ? candidate : Guid.NewGuid().ToString("N");

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var id = Resolve(context.Request.Headers[Header].FirstOrDefault());
            context.Items[ItemKey] = id;
            context.Response.Headers[Header] = id;

            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Lalana.Request");
            using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
            {
                await next(context);
            }
        });

    [GeneratedRegex("^[A-Za-z0-9-]{1,64}$")]
    private static partial Regex SafeId();
}
