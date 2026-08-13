
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class ImportWorksheetMappingStateTests
{
    [Fact]
    public void Mapping_record_preserves_selected_role()
    {
        var mapping = new ImportWorksheetMapping(
            "CREDITS",
            ImportWorksheetRole.Report,
            "User changed this from Validation.");

        Assert.Equal(ImportWorksheetRole.Report, mapping.Role);
        Assert.Equal("CREDITS", mapping.Worksheet);
    }

    [Theory]
    [InlineData(ImportWorksheetRole.Source)]
    [InlineData(ImportWorksheetRole.Validation)]
    [InlineData(ImportWorksheetRole.Report)]
    [InlineData(ImportWorksheetRole.Ignore)]
    public void All_mapping_roles_round_trip_as_enum_values(ImportWorksheetRole role)
    {
        var text = role.ToString();

        Assert.True(Enum.TryParse<ImportWorksheetRole>(text, out var parsed));
        Assert.Equal(role, parsed);
    }
}
