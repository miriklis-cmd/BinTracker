using System.Text;

namespace BinTracker.Core;

public static class ContainerTypeNameKey
{
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value
            .Trim()
            .Normalize(NormalizationForm.FormKC)
            .ToUpperInvariant();
    }
}

