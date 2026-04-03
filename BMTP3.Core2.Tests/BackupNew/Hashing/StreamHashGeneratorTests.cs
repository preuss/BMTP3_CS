using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using System.Text;

namespace BMTP3.Core2.Tests.BackupNew.Hashing;

public class StreamHashGeneratorTests
{
    [Fact]
    public async Task StreamHashGenerator_Blake3_MatchesHashCalculator()
    {
        // Arrange - sample data
        byte[] data = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog");
        var stream = new MemoryStream(data);

        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<StreamHashGenerator>();
        var gen = new StreamHashGenerator(logger);

        // Hash types to request
        var types = new[] { HashType.BLAKE3_256, HashType.BLAKE3_512 };

        // Act
        var results = await gen.ComputeHashesAsync(stream, types, null, CancellationToken.None);

        // Compute using HashCalculator for reference
        var calc = new BMTP3.Core2.BackupNew.Engine.Hashing.HashCalculator();
        string tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFile, data);
            var calcResults = calc.ComputeHashes(tempFile, types.ToList());

            // Assert
            Assert.Equal(calcResults[HashType.BLAKE3_256], results[HashType.BLAKE3_256]);
            Assert.Equal(calcResults[HashType.BLAKE3_512], results[HashType.BLAKE3_512]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
