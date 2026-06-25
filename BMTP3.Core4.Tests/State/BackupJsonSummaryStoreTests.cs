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
		BackupJsonSummaryStore store = new(_tempDir, "testsession");

		BackupSummary summary = new()
		{
			SessionId = "testsession",
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

		await store.SaveAsync(summary, default);
		BackupSummary? loaded = await store.LoadAsync(default);

		Assert.NotNull(loaded);
		Assert.Equal("testsession", loaded.SessionId);
		Assert.Single(loaded.Items);
		Assert.Equal("item1", loaded.Items[0].Id);
		Assert.Equal(BackupSummaryItemStatus.Succeeded, loaded.Items[0].Status);
	}

	[Fact]
	public async Task LoadAsync_NoFile_ReturnsNull()
	{
		BackupJsonSummaryStore store = new(_tempDir, "nonexistent");
		BackupSummary? loaded = await store.LoadAsync(default);
		Assert.Null(loaded);
	}

	[Fact]
	public async Task DeleteAsync_RemovesFile()
	{
		BackupJsonSummaryStore store = new(_tempDir, "deletable");
		await store.SaveAsync(new BackupSummary
		{
			SessionId = "deletable",
			SourceRoot = "C:\\",
			CreatedAt = DateTimeOffset.UtcNow,
			Items = new List<BackupSummaryItem>(),
		}, default);

		Assert.True(File.Exists(Path.Combine(_tempDir, "deletable.json")));

		await store.DeleteAsync();
		Assert.False(File.Exists(Path.Combine(_tempDir, "deletable.json")));
	}

	[Fact]
	public async Task DeleteAsync_NoFile_DoesNotThrow()
	{
		BackupJsonSummaryStore store = new(_tempDir, "absent");
		await store.DeleteAsync();
	}

	[Fact]
	public void StoreFile_ReturnsCorrectPath()
	{
		BackupJsonSummaryStore store = new(_tempDir, "mysession");
		Assert.NotNull(store.StoreFile);
		Assert.EndsWith("mysession.json", store.StoreFile.FullName);
	}

	[Fact]
	public void Constructor_NullDirectory_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new BackupJsonSummaryStore(null!, "s"));
	}

	[Fact]
	public void Constructor_NullSessionId_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new BackupJsonSummaryStore(_tempDir, null!));
	}

	[Fact]
	public async Task SaveAsync_AtomicWrite_NoTempFileLeft()
	{
		BackupJsonSummaryStore store = new(_tempDir, "atomic");

		await store.SaveAsync(new BackupSummary
		{
			SessionId = "atomic",
			SourceRoot = "C:\\",
			CreatedAt = DateTimeOffset.UtcNow,
			Items = new List<BackupSummaryItem>(),
		}, default);

		Assert.False(Directory.EnumerateFiles(_tempDir).Any(f => f.EndsWith(".tmp")));
	}

	public void Dispose()
	{
		if(Directory.Exists(_tempDir))
			try { Directory.Delete(_tempDir, recursive: true); } catch { }
	}
}
