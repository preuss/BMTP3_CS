using BMTP3.Core4.Engine.Downloader;
using BMTP3.Core4.Models;
using BMTP3.Core4.Tests.Fakes;

namespace BMTP3.Core4.Tests.Engine.Downloader;

public class DownloadServiceTests : IDisposable
{
	private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "BMTP3_DL_" + Guid.NewGuid().ToString("N"));
	private readonly DownloadService _service = new();

	[Fact]
	public async Task DownloadAsync_CopiesContent()
	{
		byte[] content = "Hello, World!"u8.ToArray();
		FileInfo dest = new(Path.Combine(_tempDir, "dest.txt"));
		Directory.CreateDirectory(_tempDir);

		BackupItem item = new(new FakeContent(content))
		{
			Id = "test1",
			SourcePath = "C:\\source.txt",
			RelativeFilePath = "source.txt",
			FileName = "source.txt",
			DateCreated = DateTimeOffset.UtcNow.AddDays(-1),
			DateModified = DateTimeOffset.UtcNow,
			DateAccessed = DateTimeOffset.UtcNow,
		};

		DownloadRequest request = new()
		{
			Destination = dest,
			Item = item,
			BackupStartTime = DateTimeOffset.UtcNow,
		};
		List<ulong> progress = new();

		await _service.DownloadAsync(request, new SynchronousProgress<ulong>(progress), TestContext.Current.CancellationToken);

		Assert.True(File.Exists(dest.FullName));
		Assert.Equal(content, File.ReadAllBytes(dest.FullName));
		Assert.NotEmpty(progress);
		Assert.Equal((ulong)content.Length, progress[^1]);
	}

	[Fact]
	public async Task DownloadAsync_SetsFileDates()
	{
		byte[] content = "test"u8.ToArray();
		FileInfo dest = new(Path.Combine(_tempDir, "dest.txt"));
		Directory.CreateDirectory(_tempDir);

		DateTimeOffset created = new(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
		DateTimeOffset modified = new(2025, 6, 15, 14, 30, 0, TimeSpan.Zero);
		DateTimeOffset accessed = new(2025, 6, 16, 8, 0, 0, TimeSpan.Zero);

		BackupItem item = new(new FakeContent(content))
		{
			Id = "test2",
			SourcePath = "C:\\source.txt",
			RelativeFilePath = "source.txt",
			FileName = "source.txt",
			DateCreated = created,
			DateModified = modified,
			DateAccessed = accessed,
		};

		DownloadRequest request = new()
		{
			Destination = dest,
			Item = item,
			BackupStartTime = DateTimeOffset.UtcNow,
		};
		await _service.DownloadAsync(request, null, TestContext.Current.CancellationToken);
		dest.Refresh();

		Assert.Equal(created.LocalDateTime, dest.CreationTime);
		Assert.Equal(modified.LocalDateTime, dest.LastWriteTime);
		Assert.Equal(accessed.LocalDateTime, dest.LastAccessTime);
	}

	[Fact]
	public async Task DownloadAsync_NullDates_UsesBackupStartTime()
	{
		byte[] content = "data"u8.ToArray();
		FileInfo dest = new(Path.Combine(_tempDir, "dest.txt"));
		Directory.CreateDirectory(_tempDir);

		DateTimeOffset backupStart = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);

		BackupItem item = new(new FakeContent(content))
		{
			Id = "test3",
			SourcePath = "C:\\source.txt",
			RelativeFilePath = "source.txt",
			FileName = "source.txt",
		};

		DownloadRequest request = new()
		{
			Destination = dest,
			Item = item,
			BackupStartTime = backupStart,
		};
		await _service.DownloadAsync(request, null, TestContext.Current.CancellationToken);
		dest.Refresh();

		Assert.Equal(backupStart.LocalDateTime, dest.CreationTime);
		Assert.Equal(backupStart.LocalDateTime, dest.LastWriteTime);
		Assert.Equal(backupStart.LocalDateTime, dest.LastAccessTime);
	}

	[Fact]
	public async Task DownloadAsync_ReplacesContentProvider()
	{
		byte[] content = "replace"u8.ToArray();
		FileInfo dest = new(Path.Combine(_tempDir, "dest.txt"));
		Directory.CreateDirectory(_tempDir);

		BackupItem item = new(new FakeContent(content))
		{
			Id = "test4",
			SourcePath = "C:\\source.txt",
			RelativeFilePath = "source.txt",
			FileName = "source.txt",
		};

		DownloadRequest request = new()
		{
			Destination = dest,
			Item = item,
			BackupStartTime = DateTimeOffset.UtcNow,
		};
		await _service.DownloadAsync(request, null, TestContext.Current.CancellationToken);

		IMoveableContent? moveable = item.Content as IMoveableContent;
		Assert.NotNull(moveable);
	}

	[Fact]
	public async Task DownloadAsync_ReportsProgressInChunks()
	{
		byte[] content = new byte[200_000];
		new Random(42).NextBytes(content);
		FileInfo dest = new(Path.Combine(_tempDir, "dest.bin"));
		Directory.CreateDirectory(_tempDir);

		BackupItem item = new(new FakeContent(content))
		{
			Id = "test5",
			SourcePath = "C:\\source.bin",
			RelativeFilePath = "source.bin",
			FileName = "source.bin",
		};

		List<ulong> progress = new();
		DownloadRequest request = new()
		{
			Destination = dest,
			Item = item,
			BackupStartTime = DateTimeOffset.UtcNow,
		};
		await _service.DownloadAsync(request, new SynchronousProgress<ulong>(progress), TestContext.Current.CancellationToken);

		Assert.NotEmpty(progress);
		Assert.True(progress[^1] >= (ulong)content.Length);
	}

	[Fact]
	public async Task DownloadAsync_ContentOpenReadAsyncThrows_Propagates()
	{
		var throwingContent = new ThrowingContent();
		BackupItem item = new(throwingContent)
		{
			Id = "err",
			SourcePath = "C:\\err.txt",
			RelativeFilePath = "err.txt",
			FileName = "err.txt",
		};
		FileInfo dest = new(Path.Combine(_tempDir, "dest.txt"));
		DownloadRequest request = new()
		{
			Destination = dest,
			Item = item,
			BackupStartTime = DateTimeOffset.UtcNow,
		};

		await Assert.ThrowsAsync<IOException>(() =>
			_service.DownloadAsync(request, null, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task DownloadAsync_PreCancelledToken_Throws()
	{
		byte[] content = "cancel"u8.ToArray();
		BackupItem item = new(new FakeContent(content))
		{
			Id = "cancel",
			SourcePath = "C:\\cancel.txt",
			RelativeFilePath = "cancel.txt",
			FileName = "cancel.txt",
		};
		FileInfo dest = new(Path.Combine(_tempDir, "dest.txt"));
		DownloadRequest request = new()
		{
			Destination = dest,
			Item = item,
			BackupStartTime = DateTimeOffset.UtcNow,
		};
		using CancellationTokenSource cts = new();
		cts.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			_service.DownloadAsync(request, null, cts.Token));
	}

	public void Dispose()
	{
		if(Directory.Exists(_tempDir))
			try { Directory.Delete(_tempDir, recursive: true); } catch { }
	}

	private sealed class ThrowingContent : IContent
	{
		public ulong Length => 10;
		public void Dispose() { }
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
		public Stream OpenRead() => throw new IOException("source error");
		public Task<Stream> OpenReadAsync(CancellationToken ct) => throw new IOException("source error");
	}
}
