using System.Text;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Engine.Hashing;

namespace BMTP3.Core2.Tests.BackupNew.Hashing;

public class StreamHashGenerator_MixedHashTests
{
    [Fact]
    public async Task StreamHashGenerator_MultipleHashes_MatchHashCalculator()
    {
        // Arrange
        byte[] data = Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet, consectetur adipiscing elit.");
        var stream = new MemoryStream(data);

        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<StreamHashGenerator>();
        var gen = new StreamHashGenerator(logger);

        var types = new List<HashType> { HashType.SHA2_256, HashType.BLAKE3_512 };

        // Act - stream generator
        var streamResults = await gen.ComputeHashesAsync(stream, types, null, CancellationToken.None);

        // Act - file-based HashCalculator
        var calc = new BMTP3.Core2.BackupNew.Engine.Hashing.HashCalculator();
        string temp = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(temp, data);
            var calcResults = calc.ComputeHashes(temp, types);

            // Assert both hashes match
            Assert.Equal(calcResults[HashType.SHA2_256], streamResults[HashType.SHA2_256]);
            Assert.Equal(calcResults[HashType.BLAKE3_512], streamResults[HashType.BLAKE3_512]);
        }
        finally
        {
            File.Delete(temp);
        }
    }
}
