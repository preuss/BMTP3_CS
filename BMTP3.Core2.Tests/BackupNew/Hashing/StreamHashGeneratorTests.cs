using System.Text;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core2.Tests.BackupNew.Hashing;

public class StreamHashGeneratorTests
{
	[Fact]
	public async Task StreamHashGenerator_Blake3_MatchesHashCalculator()
	{
		// Arrange - sample data
		byte[] data = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog");
		MemoryStream stream = new(data);

		NullLogger<StreamHashGenerator> logger = new();
		StreamHashGenerator gen = new(logger);

		// Hash types to request
		HashType[] types = new[] { HashType.BLAKE3_256, HashType.BLAKE3_512 };

		// Act
		Dictionary<HashType, string>
			results = await gen.ComputeHashesAsync(stream, types, null, CancellationToken.None);

		// Compute using HashCalculator for reference
		HashCalculator calc = new();
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, data);
			IReadOnlyDictionary<HashType, string> calcResults = calc.ComputeHashes(tempFile, types.ToList());

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