using System.Security.Cryptography;
using System.Text;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core2.Tests.Hashing;

public class ItemHasherTests
{
	// -----------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------

	private static ItemHasher BuildHasher()
	{
		TestLogger<ItemHasher> logger = new();
		NullLogger<StreamHashGenerator> genLogger = new();
		StreamHashGenerator generator = new(genLogger);
		return new ItemHasher(logger, generator);
	}

	/// <summary>
	///     Creates a temp file with the supplied bytes, returning its path.
	///     Caller is responsible for cleanup.
	/// </summary>
	private static string WriteTempFile(byte[] content)
	{
		string path = Path.Combine(Path.GetTempPath(), $"bmtp3_hasher_{Guid.NewGuid():N}.tmp");
		File.WriteAllBytes(path, content);
		return path;
	}

	private static BackupItem MakeItemFromFile(string filePath)
	{
		FileContent content = new(filePath);
		return BackupItem.Create(content, Path.GetFileName(filePath));
	}

	// -----------------------------------------------------------------
	// 1. ComputeHashesAsync_SHA256_ReturnsCorrectHash
	//    Known input "hello" → known SHA-256 hex string.
	// -----------------------------------------------------------------

	[Fact]
	public async Task ComputeHashesAsync_SHA256_ReturnsCorrectHash()
	{
		byte[] data = Encoding.UTF8.GetBytes("hello");
		string tempFile = WriteTempFile(data);
		try
		{
			// Compute expected SHA-256 independently via BCL
			string expectedHex;
			using (SHA256 sha = SHA256.Create())
			{
				byte[] hashBytes = sha.ComputeHash(data);
				expectedHex = Convert.ToHexString(hashBytes).ToLowerInvariant();
			}

			ItemHasher hasher = BuildHasher();
			BackupItem item = MakeItemFromFile(tempFile);

			Dictionary<HashType, string> result = await hasher.ComputeHashesAsync(
				item,
				new List<HashType> { HashType.SHA2_256 },
				null!,
				CancellationToken.None);

			Assert.True(result.ContainsKey(HashType.SHA2_256));
			Assert.Equal(expectedHex, result[HashType.SHA2_256]);
		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch
			{
			}
		}
	}

	// -----------------------------------------------------------------
	// 2. ComputeHashesAsync_EmptyHashTypeList_DefaultsToSHA256
	//    When hashTypes is empty, ItemHasher defaults to SHA2_256.
	// -----------------------------------------------------------------

	[Fact]
	public async Task ComputeHashesAsync_EmptyHashTypeList_DefaultsToSHA256()
	{
		byte[] data = Encoding.UTF8.GetBytes("default hash test");
		string tempFile = WriteTempFile(data);
		try
		{
			ItemHasher hasher = BuildHasher();
			BackupItem item = MakeItemFromFile(tempFile);

			Dictionary<HashType, string> result = await hasher.ComputeHashesAsync(
				item,
				new List<HashType>(), // empty → should default to SHA2_256
				null!,
				CancellationToken.None);

			// ItemHasher adds SHA2_256 as the default when the list is empty
			Assert.NotEmpty(result);
			Assert.True(result.ContainsKey(HashType.SHA2_256), "Expected SHA2_256 as the default hash type");
			Assert.False(string.IsNullOrWhiteSpace(result[HashType.SHA2_256]));
		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch
			{
			}
		}
	}

	// -----------------------------------------------------------------
	// 3. ComputeHashesAsync_MultipleTypes_ReturnsAll
	//    Requesting SHA2_256 + MD5_128 should return both keys.
	// -----------------------------------------------------------------

	[Fact]
	public async Task ComputeHashesAsync_MultipleTypes_ReturnsAll()
	{
		byte[] data = Encoding.UTF8.GetBytes("multi-hash content");
		string tempFile = WriteTempFile(data);
		try
		{
			ItemHasher hasher = BuildHasher();
			BackupItem item = MakeItemFromFile(tempFile);

			List<HashType> requested = new() { HashType.SHA2_256, HashType.MD5_128 };

			Dictionary<HashType, string> result = await hasher.ComputeHashesAsync(
				item,
				requested,
				null!,
				CancellationToken.None);

			Assert.True(result.ContainsKey(HashType.SHA2_256), "SHA2_256 should be present");
			Assert.True(result.ContainsKey(HashType.MD5_128), "MD5_128 should be present");
			Assert.False(string.IsNullOrWhiteSpace(result[HashType.SHA2_256]));
			Assert.False(string.IsNullOrWhiteSpace(result[HashType.MD5_128]));
			// The two hashes should be different values
			Assert.NotEqual(result[HashType.SHA2_256], result[HashType.MD5_128]);
		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch
			{
			}
		}
	}

	// -----------------------------------------------------------------
	// 4. ComputeHashesAsync_NonExistentFile_Throws
	//    FileContent constructor throws FileNotFoundException for missing
	//    files, so the item cannot even be constructed – verify this.
	// -----------------------------------------------------------------

	[Fact]
	public void ComputeHashesAsync_NonExistentFile_ThrowsFileNotFoundException()
	{
		string nonExistent = Path.Combine(Path.GetTempPath(), $"bmtp3_missing_{Guid.NewGuid():N}.tmp");
		Assert.False(File.Exists(nonExistent), "Pre-condition: file must not exist");

		// FileContent throws at construction time if the file doesn't exist
		Assert.Throws<FileNotFoundException>(() => { _ = new FileContent(nonExistent); });
	}
}