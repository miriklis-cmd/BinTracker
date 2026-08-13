
namespace BinTracker.Services;

public enum LegacyContainerResolutionKind
{
    ExplicitKnown = 0,
    DefaultBlue = 1,
    UnknownExplicitToken = 2,
    ManualMapping = 3
}

public sealed record LegacyContainerResolution(
    string RawHint,
    string DisplayName,
    int? ContainerTypeId,
    bool IsResolved,
    LegacyContainerResolutionKind Kind,
    string Reason);

public static class LegacyContainerHintResolver
{
    /// <summary>
    /// Jack Miriklis legacy workbook rules:
    ///
    /// - No bracket/container token => Blue Bin (industry-standard default).
    /// - Y => Yellow Bin.
    /// - Bulk => Bulk Bin.
    /// - Otherwise try current Container Type Name or ShortCode.
    /// - Unknown explicit tokens are NEVER guessed. They remain unresolved
    ///   and must be mapped before import (e.g. "(Tub) Customer").
    /// </summary>
    public static LegacyContainerResolution Resolve(
        string? hint,
        IReadOnlyCollection<ContainerTypeListRow> containerTypes,
        IReadOnlyDictionary<string, int>? manualMappings = null)
    {
        var raw = (hint ?? string.Empty).Trim();

        if (raw.Length > 0 &&
            TryGetManualMapping(raw, manualMappings, out var mappedContainerTypeId))
        {
            var mapped = containerTypes.FirstOrDefault(x => x.Id == mappedContainerTypeId);

            if (mapped is not null)
            {
                return new LegacyContainerResolution(
                    raw,
                    mapped.Name,
                    mapped.Id,
                    true,
                    LegacyContainerResolutionKind.ManualMapping,
                    $"Legacy token '{raw}' manually mapped to {mapped.Name} for this import.");
            }
        }

        if (raw.Length == 0)
        {
            var blue = FindByNameOrCode(containerTypes, "Blue Bin", "BLUE");

            if (blue is not null)
            {
                return new LegacyContainerResolution(
                    raw,
                    blue.Name,
                    blue.Id,
                    true,
                    LegacyContainerResolutionKind.DefaultBlue,
                    "No legacy container token; defaulted to standard Blue Bin.");
            }

            return new LegacyContainerResolution(
                raw,
                "Blue Bin",
                null,
                false,
                LegacyContainerResolutionKind.DefaultBlue,
                "No legacy container token; Blue Bin is the default but is not configured in BinTracker.");
        }

        var aliasTarget = raw.Equals("Y", StringComparison.OrdinalIgnoreCase)
            ? "Yellow Bin"
            : raw.Equals("Bulk", StringComparison.OrdinalIgnoreCase)
                ? "Bulk Bin"
                : null;

        ContainerTypeListRow? match = null;

        if (aliasTarget is not null)
        {
            match = containerTypes.FirstOrDefault(x =>
                x.Name.Equals(aliasTarget, StringComparison.OrdinalIgnoreCase));
        }

        match ??= containerTypes.FirstOrDefault(x =>
            x.Name.Equals(raw, StringComparison.OrdinalIgnoreCase) ||
            x.ShortCode.Equals(raw, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            return new LegacyContainerResolution(
                raw,
                match.Name,
                match.Id,
                true,
                LegacyContainerResolutionKind.ExplicitKnown,
                $"Legacy token '{raw}' resolved to {match.Name}.");
        }

        if (aliasTarget is not null)
        {
            return new LegacyContainerResolution(
                raw,
                aliasTarget,
                null,
                false,
                LegacyContainerResolutionKind.ExplicitKnown,
                $"Legacy token '{raw}' means {aliasTarget}, but that Container Type is not configured.");
        }

        return new LegacyContainerResolution(
            raw,
            raw,
            null,
            false,
            LegacyContainerResolutionKind.UnknownExplicitToken,
            $"Unknown legacy container token '{raw}'. Map it to an existing Container Type or create one before import.");
    }

    private static bool TryGetManualMapping(
        string raw,
        IReadOnlyDictionary<string, int>? manualMappings,
        out int containerTypeId)
    {
        containerTypeId = 0;
        if (manualMappings is null) return false;

        foreach (var pair in manualMappings)
        {
            if (!pair.Key.Equals(raw, StringComparison.OrdinalIgnoreCase)) continue;
            containerTypeId = pair.Value;
            return true;
        }

        return false;
    }

    private static ContainerTypeListRow? FindByNameOrCode(
        IReadOnlyCollection<ContainerTypeListRow> containerTypes,
        string name,
        string shortCode) =>
        containerTypes.FirstOrDefault(x =>
            x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            x.ShortCode.Equals(shortCode, StringComparison.OrdinalIgnoreCase));
}
