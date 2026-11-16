using System.Reflection;

namespace InvestmentCoPilot.Host.OpenApi;

internal static class DocumentGeneration
{
    /// <summary>
    /// True when the app is started by the build-time OpenAPI generator rather than to serve requests.
    /// The generator runs without real configuration, so startup validation and migrations are skipped.
    /// </summary>
    public static bool IsRunning { get; } =
        Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
}
