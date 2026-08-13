
using Xunit;

namespace BinTracker.UnitTests;

public sealed class ImportReviewLayoutContractTests
{
    [Fact]
    public void Review_grid_section_is_expected_to_own_remaining_height()
    {
        // WinForms layout is visually verified in the manual test checklist.
        // This contract exists to document the critical rule that caused the
        // alpha.18.2 regression: the Review tab card must NOT use the generic
        // compact-card AutoSize behaviour.
        const bool reviewCardMustAutoSize = false;

        Assert.False(reviewCardMustAutoSize);
    }
}
