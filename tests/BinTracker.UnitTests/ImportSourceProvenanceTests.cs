
using Xunit;

namespace BinTracker.UnitTests;

public sealed class ImportSourceProvenanceTests
{
    [Fact]
    public void Sha256_hex_fingerprint_is_64_characters()
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("BinTracker import provenance test"));

        var value = Convert.ToHexString(bytes);

        Assert.Equal(64, value.Length);
        Assert.Matches("^[0-9A-F]{64}$", value);
    }
}
