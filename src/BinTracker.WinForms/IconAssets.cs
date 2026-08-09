using System.Reflection;

namespace BinTracker.WinForms;

/// <summary>
/// Loads small PNG assets embedded in the WinForms assembly.
/// Embedded resources keep BinTracker self-contained and ensure icons render
/// consistently regardless of the fonts installed on the workstation.
/// </summary>
internal static class IconAssets
{
    private static readonly Dictionary<string, Image> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static Image Get(string name)
    {
        if (Cache.TryGetValue(name, out var cached))
            return cached;

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"BinTracker.WinForms.Assets.{name}.png";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded icon '{name}' was not found.");

        using var source = Image.FromStream(stream);
        var image = new Bitmap(source);
        Cache[name] = image;
        return image;
    }
}
