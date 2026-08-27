using BinTracker.Core;
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class MovementChangeComparisonTests
{
    [Fact]
    public void Quantity_only_reports_exact_quantity_and_omits_unchanged_fields()
    {
        var differences = MovementChangeComparison.Compare(Lines(Line(quantity: 7), Line(quantity: 5, role: "Corrected replacement")));
        var difference = Assert.Single(differences);
        Assert.Equal("Quantity: 7 → 5", difference.Display);
    }

    [Theory]
    [InlineData("date", "Date: 25/08/2026 → 26/08/2026")]
    [InlineData("direction", "Direction: OUT → IN")]
    [InlineData("customer", "Customer: ABC — Alpha → XYZ — Xylophone")]
    [InlineData("container", "Container: Blue Bin → Yellow Bin")]
    public void Single_field_changes_are_named_exactly(string field, string expected)
    {
        var before = Line();
        var after = field switch
        {
            "date" => Line(role: "Corrected replacement", date: new(2026, 8, 26)),
            "direction" => Line(role: "Corrected replacement", direction: MovementType.In),
            "customer" => Line(role: "Corrected replacement", code: "XYZ", customer: "Xylophone"),
            _ => Line(role: "Corrected replacement", container: "Yellow Bin")
        };
        Assert.Equal(expected, Assert.Single(MovementChangeComparison.Compare(Lines(before, after))).Display);
    }

    [Fact]
    public void Multiple_changes_are_clear_and_unchanged_reference_and_notes_are_omitted()
    {
        var differences = MovementChangeComparison.Compare(Lines(
            Line(quantity: 7),
            Line(role: "Corrected replacement", quantity: 5, direction: MovementType.In, reference: "new")));
        Assert.Equal(new[] { "Direction", "Quantity", "Reference" }, differences.Select(x => x.Field));
        Assert.DoesNotContain(differences, x => x.Field == "Notes");
    }

    [Fact]
    public void Batch_comparison_reports_only_shared_date_and_direction_changes()
    {
        var lines = Lines(
            Line(id: 1), Line(id: 2),
            Line(id: 3, role: "Corrected replacement", date: new(2026, 8, 26), direction: MovementType.In),
            Line(id: 4, role: "Corrected replacement", date: new(2026, 8, 26), direction: MovementType.In));
        Assert.Equal(new[] { "Date", "Direction" }, MovementChangeComparison.Compare(lines).Select(x => x.Field));
    }

    [Fact]
    public void Human_facing_action_label_does_not_mutate_stored_action()
    {
        const string stored = "MOVEMENT_CORRECTED";
        var row = new AuditTrailRow(1, DateTime.UtcNow, "jack", stored, "BinMovement", "1040", "change", true, true, null, null, null);
        Assert.Equal("Movement corrected", row.ActionDisplay);
        Assert.Equal(stored, row.Action);
    }

    private static IReadOnlyList<MovementChangeAuditLine> Lines(params MovementChangeAuditLine[] lines) => lines;
    private static MovementChangeAuditLine Line(long id = 1, string role = "Original",
        DateOnly? date = null, string code = "ABC", string customer = "Alpha", string container = "Blue Bin",
        MovementType direction = MovementType.Out, int quantity = 7, string reference = "old", string notes = "same") =>
        new(role, id, 19, date ?? new DateOnly(2026, 8, 25), code, customer, container, direction,
            quantity, reference, notes, null);
}
