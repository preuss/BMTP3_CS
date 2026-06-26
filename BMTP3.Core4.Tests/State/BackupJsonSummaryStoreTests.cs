using BMTP3.Core4.State;

namespace BMTP3.Core4.Tests.State;

public class BackupJsonSummaryStoreTests : IDisposable
{
	private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "BMTP3_SUM_" + Guid.NewGuid().ToString("N"));

	public BackupJsonSummaryStoreTests()
	{
		Directory.CreateDirectory(_tempDir);
	}

	[Fact]
	public async Task SaveAndLoad_Roundtrip()
	{
		BackupJsonSummaryStore store = new(_tempDir, "abc123def4567");

		BackupSummary summary = new()
		{
			SessionId = "abc123def4567",
			SourceRoot = "FileSystem:C:\\src",
			CreatedAt = new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero),
			Items = new List<BackupSummaryItem>
			{
				new()
				{
					Id = "item1",
					SourcePath = "C:\\file1.jpg",
					RelativeFilePath = "file1.jpg",
					FileName = "file1.jpg",
					Length = 100,
					Status = BackupSummaryItemStatus.Succeeded,
					IsCompleted = true,
				},
			},
		};

		await store.SaveAsync(summary, TestContext.Current.CancellationToken);
		BackupSummary? loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

		Assert.NotNull(loaded);
		Assert.Equal("abc123def4567", loaded.SessionId);
		Assert.Single(loaded.Items);
		Assert.Equal("item1", loaded.Items[0].Id);
		Assert.Equal(BackupSummaryItemStatus.Succeeded, loaded.Items[0].Status);
	}

	[Fact]
	public async Task LoadAsync_NoFile_ReturnsNull()
	{
		BackupJsonSummaryStore store = new(_tempDir, "nonexistent12");
		BackupSummary? loaded = await store.LoadAsync(TestContext.Current.CancellationToken);
		Assert.Null(loaded);
	}

	[Fact]
	public async Task DeleteAsync_RemovesFile()
	{
		BackupJsonSummaryStore store = new(_tempDir, "deletableid123");
		await store.SaveAsync(new BackupSummary
		{
			SessionId = "deletableid123",
			SourceRoot = "C:\\",
			CreatedAt = DateTimeOffset.UtcNow,
			Items = new List<BackupSummaryItem>(),
		}, TestContext.Current.CancellationToken);

		Assert.True(File.Exists(Path.Combine(_tempDir, "session_deletableid1.json")));

		await store.DeleteAsync();
		Assert.False(File.Exists(Path.Combine(_tempDir, "session_deletableid1.json")));
	}

	[Fact]
	public async Task DeleteAsync_NoFile_DoesNotThrow()
	{
		BackupJsonSummaryStore store = new(_tempDir, "absentid12345");
		await store.DeleteAsync();
	}

	[Fact]
	public void StoreFile_ReturnsCorrectPath()
	{
		BackupJsonSummaryStore store = new(_tempDir, "mysessionid123");
		Assert.NotNull(store.StoreFile);
		Assert.Equal(
			Path.Combine(_tempDir, "session_mysessionid1.json"),
			store.StoreFile.FullName);
	}

	[Fact]
	public void Constructor_NullDirectory_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new BackupJsonSummaryStore(null!, "sp"));
	}

	[Fact]
	public void Constructor_NullSessionId_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new BackupJsonSummaryStore(_tempDir, null!));
	}

	[Fact]
	public async Task SaveAsync_AtomicWrite_NoTempFileLeft()
	{
		BackupJsonSummaryStore store = new(_tempDir, "atomicid12345");

		await store.SaveAsync(new BackupSummary
		{
			SessionId = "atomicid12345",
			SourceRoot = "C:\\",
			CreatedAt = DateTimeOffset.UtcNow,
			Items = new List<BackupSummaryItem>(),
		}, TestContext.Current.CancellationToken);

		Assert.DoesNotContain(Directory.EnumerateFiles(_tempDir), f => f.EndsWith(".tmp"));
	}

	public void Dispose()
	{
		if(Directory.Exists(_tempDir))
			try { Directory.Delete(_tempDir, recursive: true); } catch { }
	}
}
