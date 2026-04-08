using System.Text;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.Tests.Utils;

namespace BMTP3.Core2.Tests.Strategies;

/// <summary>
///     No-op waterfall stub – simply records that it was called without touching metadata.
/// </summary>
internal sealed class NoOpTimestampWaterfall : ITimestampWaterfall
{
	public bool WasCalled { get; private set; }

	public void Apply(IBackupItem item)
	{
		WasCalled = true;
	}
}

public class MetadataReaderTests
{
	// -----------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------

	private static MetadataReader BuildReader(out NoOpTimestampWaterfall waterfall)
	{
		TestLogger<MetadataReader> logger = new();
		waterfall = new NoOpTimestampWaterfall();
		return new MetadataReader(logger, waterfall);
	}

	private static string WriteTempFile(string extension, byte[] content)
	{
		string path = Path.Combine(
			Path.GetTempPath(),
			$"bmtp3_meta_{Guid.NewGuid():N}{extension}");
		File.WriteAllBytes(path, content);
		return path;
	}

	private static BackupItem MakeItemFromFile(string filePath)
	{
		FileContent fc = new(filePath);
		return BackupItem.Create(fc, Path.GetFileName(filePath));
	}

	// -----------------------------------------------------------------
	// 1. EnrichMetadataAsync_UnsupportedExtension_DoesNotThrow
	//    A .xyz file with arbitrary text content should be handled
	//    gracefully – no exception should propagate.
	// -----------------------------------------------------------------

	[Fact]
	public async Task EnrichMetadataAsync_UnsupportedExtension_DoesNotThrow()
	{
		byte[] content = Encoding.UTF8.GetBytes("some arbitrary text content for an unsupported extension");
		string tempFile = WriteTempFile(".xyz", content);
		try
		{
			MetadataReader reader = BuildReader(out _);
			BackupItem item = MakeItemFromFile(tempFile);

			// Should not throw for an unsupported extension
			await reader.EnrichMetadataAsync(item, CancellationToken.None);
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
	// 2. EnrichMetadataAsync_CorruptFile_DoesNotThrow
	//    A file named .jpg that contains garbage binary data should be
	//    handled gracefully by the MetadataExtractor catch block.
	// -----------------------------------------------------------------

	[Fact]
	public async Task EnrichMetadataAsync_CorruptFile_DoesNotThrow()
	{
		// Garbage binary bytes – definitively not a valid JPEG
		byte[] garbage = new byte[512];
		Random rng = new(42);
		rng.NextBytes(garbage);

		string tempFile = WriteTempFile(".jpg", garbage);
		try
		{
			MetadataReader reader = BuildReader(out _);
			BackupItem item = MakeItemFromFile(tempFile);

			// MetadataExtractor will fail to parse but the reader should swallow the exception
			await reader.EnrichMetadataAsync(item, CancellationToken.None);
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
	// 3. EnrichMetadataAsync_ValidFile_SetsLengthMetadata
	//    After enrichment the Length metadata key must be set to the
	//    file's byte count. We use a plain .txt file to keep the test
	//    simple and dependency-free.
	// -----------------------------------------------------------------

	[Fact]
	public async Task EnrichMetadataAsync_ValidFile_SetsLengthMetadata()
	{
		byte[] content = Encoding.UTF8.GetBytes("Hello, metadata!");
		string tempFile = WriteTempFile(".txt", content);
		try
		{
			MetadataReader reader = BuildReader(out NoOpTimestampWaterfall waterfall);
			BackupItem item = MakeItemFromFile(tempFile);

			await reader.EnrichMetadataAsync(item, CancellationToken.None);

			// Length should be present – set either by BackupItem.Create or by the reader itself
			Assert.True(item.Metadata.Has(MetadataKey.Length),
				"MetadataKey.Length should be set after EnrichMetadataAsync");

			ulong storedLength = item.Metadata.Get<ulong>(MetadataKey.Length);
			Assert.Equal((ulong)content.Length, storedLength);

			// The timestamp waterfall must always be applied, regardless of file type
			Assert.True(waterfall.WasCalled, "ITimestampWaterfall.Apply should always be called");
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
}