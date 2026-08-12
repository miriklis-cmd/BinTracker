using System.Reflection;

namespace BinTracker.WinForms;

/// <summary>
/// Provides the running application's release version from assembly metadata.
/// The source value is defined once in Directory.Build.props.
/// </summary>
internal static class AppVersion
{
    private static readonly Lazy<string> CurrentValue = new(ReadVersion);

    public static string Current => CurrentValue.Value;

    public static string Display =>
        Current.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? Current
            : $"v{Current}";

    private static string ReadVersion()
    {
        var assembly = typeof(AppVersion).Assembly;

        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        // Some build systems append +commit metadata. Keep the user-facing
        // version concise while retaining the assembly metadata internally.
        var version = informational?
            .Split('+', 2, StringSplitOptions.TrimEntries)[0]
            .Trim();

        if (!string.IsNullOrWhiteSpace(version))
            return version;

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}
