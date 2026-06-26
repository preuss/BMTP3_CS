using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Steps.TimestampCorrectionStep;

namespace BMTP3.Core2.Tests.Steps;

public class TimestampCorrectionItemStepTests
{
	// ── Helpers ──────────────────────────────────────────────────────────────

	private static TimestampCorrectionItemStep BuildStep()
	{
		return new TimestampCorrectionItemStep(new BackupPlan());
	}

	// ── Tests ────────────────────────────────────────────────────────────────

	[Fact]
	public async Task ExecuteAsync_SetsFileTimestamps_FromAuthoredDateTime()
	{
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3 });

			FileContent content = new(tempFile);
			BackupItem item = BackupItem.Create(content, Path.GetFileName(tempFile));

			// Set an authored timestamp that is clearly in the past
			DateTime authored = new(2010, 6, 15, 12, 0, 0, DateTimeKind.Utc);
			item.Metadata.Set(MetadataKey.AuthoredDateTime, authored);

			TimestampCorrectionItemStep step = BuildStep();
			await step.ExecuteAsync(item, null!, CancellationToken.None);

			// Both creation and last-write times must have been updated to the authored date
			FileInfo fileInfo = new(tempFile);
			Assert.Equal(authored, fileInfo.LastWriteTimeUtc, TimeSpan.FromSeconds(2));
			Assert.Equal(authored, fileInfo.CreationTimeUtc, TimeSpan.FromSeconds(2));
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

	[Fact]
	public async Task ExecuteAsync_FallsBackToCreatedDateTime_WhenAuthoredIsNull()
	{
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[] { 4, 5, 6 });

			FileContent content = new(tempFile);
			BackupItem item = BackupItem.Create(content, Path.GetFileName(tempFile));

			// Only CreatedDateTime is present – no AuthoredDateTime
			DateTime created = new(2015, 3, 20, 8, 30, 0, DateTimeKind.Utc);
			item.Metadata.Set(MetadataKey.CreatedDateTime, created);

			TimestampCorrectionItemStep step = BuildStep();
			await step.ExecuteAsync(item, null!, CancellationToken.None);

			FileInfo fileInfo = new(tempFile);
			Assert.Equal(created, fileInfo.LastWriteTimeUtc, TimeSpan.FromSeconds(2));
			Assert.Equal(created, fileInfo.CreationTimeUtc, TimeSpan.FromSeconds(2));
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

	[Fact]
	public async Task ExecuteAsync_SkipsNonFileContent_WithoutError()
	{
		// An item whose content is NOT a FileContent should pass through with no exception.
		MemoryContent content = new(new byte[] { 7, 8, 9 });
		BackupItem item = BackupItem.Create(content, "in-memory-item.bin");

		DateTime authored = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		item.Metadata.Set(MetadataKey.AuthoredDateTime, authored);

		TimestampCorrectionItemStep step = BuildStep();

		// Must not throw
		bool result = await step.ExecuteAsync(item, null!, CancellationToken.None);

		Assert.True(result);
		// Item must not have been marked failed
		Assert.Equal(ItemResultState.Pending, item.ResultState);
	}

	[Fact]
	public async Task ExecuteAsync_DoesNotThrow_WhenNoTimestampMetadata()
	{
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[] { 11, 22, 33 });

			FileContent content = new(tempFile);
			BackupItem item = BackupItem.Create(content, Path.GetFileName(tempFile));

			// No timestamp keys are set at all – step should be a no-op
			TimestampCorrectionItemStep step = BuildStep();
			bool result = await step.ExecuteAsync(item, null!, CancellationToken.None);

			Assert.True(result);
			Assert.Equal(ItemResultState.Pending, item.ResultState);
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

	/// <summary>
	///     An in-memory IContent implementation that is NOT a FileContent.
	///     Used to verify the step silently skips non-file content.
	/// </summary>
	private sealed class MemoryContent : IContent
	{
		private readonly byte[] _data;

		public MemoryContent(byte[] data)
		{
			_data = data;
		}

		public ulong Length => (ulong)_data.Length;

		public Stream OpenRead()
		{
			return new MemoryStream(_data, false);
		}

		public Task<Stream> OpenReadStreamAsync(CancellationToken ct)
		{
			return Task.FromResult<Stream>(new MemoryStream(_data, false));
		}

		public void Dispose()
		{
		}
	}
}