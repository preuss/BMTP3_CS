using System.Text;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core2.Tests.BackupNew.Hashing;

public class StreamHashGenerator_MixedHashTests
{
	[Fact]
	public async Task StreamHashGenerator_MultipleHashes_MatchHashCalculator()
	{
		// Arrange
		byte[] data = Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet, consectetur adipiscing elit.");
		MemoryStream stream = new(data);

		NullLogger<StreamHashGenerator> logger = new();
		StreamHashGenerator gen = new(logger);

		List<HashType> types = new() { HashType.SHA2_256, HashType.BLAKE3_512 };

		// Act - stream generator
		Dictionary<HashType, string> streamResults =
			await gen.ComputeHashesAsync(stream, types, null, CancellationToken.None);

		// Act - file-based HashCalculator
		HashCalculator calc = new();
		string temp = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(temp, data);
			IReadOnlyDictionary<HashType, string> calcResults = calc.ComputeHashes(temp, types);

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