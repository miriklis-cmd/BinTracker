using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class ImportWorksheetRoleTests
{
    [Theory]
    [InlineData(ImportWorksheetRole.Source)]
    [InlineData(ImportWorksheetRole.Validation)]
    [InlineData(ImportWorksheetRole.Report)]
    [InlineData(ImportWorksheetRole.Ignore)]
    public void Worksheet_role_values_round_trip(ImportWorksheetRole role)
    {
        var text = role.ToString();
        Assert.True(Enum.TryParse<ImportWorksheetRole>(text, out var parsed));
        Assert.Equal(role, parsed);
    }
}
