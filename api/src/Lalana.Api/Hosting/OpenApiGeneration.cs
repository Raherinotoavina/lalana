using System.Reflection;

namespace Lalana.Api.Hosting;

internal static class OpenApiGeneration
{
    /// <summary>
    /// True while <c>dotnet build</c> generates the OpenAPI document: the app is started by the
    /// GetDocument tool with a mock server and no configuration, so startup checks are skipped.
    /// </summary>
    public static bool IsRunning { get; } =
        Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
}
