using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class LegacyContainerHintResolverTests
{
    private static readonly ContainerTypeListRow[] Types =
    [
        new(1, "Blue Bin", "BLUE", 1, true, false, 0),
        new(2, "Yellow Bin", "YB", 2, true, false, 0),
        new(3, "Bulk Bin", "BULK", 3, true, false, 0),
        new(4, "CHEP Pallet", "CHEP", 4, true, true, 0)
    ];

    [Theory]
    [InlineData("Y", "Yellow Bin", 2)]
    [InlineData("Bulk", "Bulk Bin", 3)]
    [InlineData("CHEP", "CHEP Pallet", 4)]
    public void Confirmed_aliases_and_short_codes_resolve(
        string hint,
        string expectedName,
        int expectedId)
    {
        var result = LegacyContainerHintResolver.Resolve(hint, Types);

        Assert.True(result.IsResolved);
        Assert.Equal(expectedName, result.DisplayName);
        Assert.Equal(expectedId, result.ContainerTypeId);
    }

    [Fact]
    public void No_hint_defaults_to_blue_bin()
    {
        var result = LegacyContainerHintResolver.Resolve(null, Types);

        Assert.True(result.IsResolved);
        Assert.Equal("Blue Bin", result.DisplayName);
        Assert.Equal(LegacyContainerResolutionKind.DefaultBlue, result.Kind);
    }

    [Fact]
    public void Unknown_explicit_hint_is_not_guessed()
    {
        var result = LegacyContainerHintResolver.Resolve("Tub", Types);

        Assert.False(result.IsResolved);
        Assert.Null(result.ContainerTypeId);
        Assert.Equal("Tub", result.DisplayName);
        Assert.Equal(LegacyContainerResolutionKind.UnknownExplicitToken, result.Kind);
        Assert.Contains("Unknown legacy container token", result.Reason);
    }


    [Fact]
    public void Manual_mapping_resolves_unknown_explicit_token()
    {
        var result = LegacyContainerHintResolver.Resolve(
            "Tub", Types,
            new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase) { ["Tub"] = 3 });

        Assert.True(result.IsResolved);
        Assert.Equal(3, result.ContainerTypeId);
        Assert.Equal("Bulk Bin", result.DisplayName);
        Assert.Equal(LegacyContainerResolutionKind.ManualMapping, result.Kind);
    }
}
