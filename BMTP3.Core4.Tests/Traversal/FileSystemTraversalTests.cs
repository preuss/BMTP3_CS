using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Helpers;
using BMTP3.Core4.Tests.Fakes;
using BMTP3.Core4.Traversal;

namespace BMTP3.Core4.Tests.Traversal;

public class FileSystemTraversalTests : IDisposable
{
	private readonly string _rootDir = Path.Combine(Path.GetTempPath(), "BMTP3_FST_" + Guid.NewGuid().ToString("N"));

	public FileSystemTraversalTests()
	{
		Directory.CreateDirectory(_rootDir);
	}

	[Fact]
	public async Task TraverseAsync_SingleFile_ReturnsItem()
	{
		File.WriteAllText(Path.Combine(_rootDir, "photo.jpg"), "content");

		SourceTraversalRequest request = new()
		{
			SourcePath = PathHelper.ToInternalCanonicalUri(_rootDir, BackupSourceType.FileSystem),
			Recursive = false,
		};

		FileSystemTraversal traversal = new();
		List<SourceTraversalItem> items = await CollectAsync(traversal.TraverseAsync(request, null, TestContext.Current.CancellationToken));

		Assert.Single(items);
		Assert.Equal("photo.jpg", items[0].FileName);
		Assert.EndsWith("photo.jpg", items[0].SourcePath);
	}

	[Fact]
	public async Task TraverseAsync_Recursive_ReturnsAllFiles()
	{
		File.WriteAllText(Path.Combine(_rootDir, "root.txt"), "root");
		Directory.CreateDirectory(Path.Combine(_rootDir, "sub"));
		File.WriteAllText(Path.Combine(_rootDir, "sub", "sub.txt"), "sub");

		SourceTraversalRequest request = new()
		{
			SourcePath = PathHelper.ToInternalCanonicalUri(_rootDir, BackupSourceType.FileSystem),
			Recursive = true,
		};

		FileSystemTraversal traversal = new();
		List<SourceTraversalItem> items = await CollectAsync(traversal.TraverseAsync(request, null, TestContext.Current.CancellationToken));

		Assert.Equal(2, items.Count);
		Assert.Contains(items, i => i.RelativeFilePath == "root.txt");
		Assert.Contains(items, i => i.RelativeFilePath == "sub/sub.txt");
	}

	[Fact]
	public async Task TraverseAsync_NonRecursive_ReturnsOnlyRootFiles()
	{
		File.WriteAllText(Path.Combine(_rootDir, "root.txt"), "root");
		Directory.CreateDirectory(Path.Combine(_rootDir, "sub"));
		File.WriteAllText(Path.Combine(_rootDir, "sub", "sub.txt"), "sub");

		SourceTraversalRequest request = new()
		{
			SourcePath = PathHelper.ToInternalCanonicalUri(_rootDir, BackupSourceType.FileSystem),
			Recursive = false,
		};

		FileSystemTraversal traversal = new();
		List<SourceTraversalItem> items = await CollectAsync(traversal.TraverseAsync(request, null, TestContext.Current.CancellationToken));

		Assert.Single(items);
		Assert.Equal("root.txt", items[0].FileName);
	}

	[Fact]
	public async Task TraverseAsync_WithProgress_ReportsProgress()
	{
		File.WriteAllText(Path.Combine(_rootDir, "f1.txt"), "a");
		File.WriteAllText(Path.Combine(_rootDir, "f2.txt"), "b");

		List<SourceTraversalProgress> progress = new();

		SourceTraversalRequest request = new()
		{
			SourcePath = PathHelper.ToInternalCanonicalUri(_rootDir, BackupSourceType.FileSystem),
			Recursive = false,
		};

		FileSystemTraversal traversal = new();
		await CollectAsync(traversal.TraverseAsync(request, new SynchronousProgress<SourceTraversalProgress>(progress), TestContext.Current.CancellationToken));

		Assert.NotEmpty(progress);
	}

	[Fact]
	public async Task TraverseAsync_NonexistentSource_Throws()
	{
		SourceTraversalRequest request = new()
		{
			SourcePath = "file:///Z:/nonexistent_path_12345",
			Recursive = false,
		};

		FileSystemTraversal traversal = new();
		await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
			CollectAsync(traversal.TraverseAsync(request, null, TestContext.Current.CancellationToken)));
	}

	[Fact]
	public async Task TraverseAsync_NullRequest_Throws()
	{
		FileSystemTraversal traversal = new();
		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			CollectAsync(traversal.TraverseAsync(null!, null, TestContext.Current.CancellationToken)));
	}

	[Fact]
	public async Task TraverseAsync_Cancelled_Throws()
	{
		File.WriteAllText(Path.Combine(_rootDir, "f.txt"), "data");

		using CancellationTokenSource cts = new();
		cts.Cancel();

		SourceTraversalRequest request = new()
		{
			SourcePath = PathHelper.ToInternalCanonicalUri(_rootDir, BackupSourceType.FileSystem),
			Recursive = false,
		};

		FileSystemTraversal traversal = new();
		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			CollectAsync(traversal.TraverseAsync(request, null, cts.Token)));
	}

	[Fact]
	public async Task TraverseAsync_EmptyDirectory_ReturnsEmpty()
	{
		SourceTraversalRequest request = new()
		{
			SourcePath = PathHelper.ToInternalCanonicalUri(_rootDir, BackupSourceType.FileSystem),
			Recursive = true,
		};

		FileSystemTraversal traversal = new();
		List<SourceTraversalItem> items = await CollectAsync(traversal.TraverseAsync(request, null, TestContext.Current.CancellationToken));

		Assert.Empty(items);
	}

	[Fact]
	public async Task TraverseAsync_ItemId_HasCorrectValue()
	{
		File.WriteAllText(Path.Combine(_rootDir, "test.jpg"), "data");

		SourceTraversalRequest request = new()
		{
			SourcePath = PathHelper.ToInternalCanonicalUri(_rootDir, BackupSourceType.FileSystem),
			Recursive = false,
		};

		FileSystemTraversal traversal = new();
		List<SourceTraversalItem> items = await CollectAsync(traversal.TraverseAsync(request, null, TestContext.Current.CancellationToken));

		Assert.Single(items);
		Assert.EndsWith("test.jpg", items[0].Id);
		Assert.Equal(PathHelper.ToInternalCanonicalUri(Path.Combine(_rootDir, "test.jpg"), BackupSourceType.FileSystem), items[0].SourcePath);
	}

	[Fact]
	public async Task TraverseAsync_FileContent_HasCorrectLength()
	{
		byte[] data = new byte[5_000];
		new Random(42).NextBytes(data);
		File.WriteAllBytes(Path.Combine(_rootDir, "data.bin"), data);

		SourceTraversalRequest request = new()
		{
			SourcePath = PathHelper.ToInternalCanonicalUri(_rootDir, BackupSourceType.FileSystem),
			Recursive = false,
		};

		FileSystemTraversal traversal = new();
		List<SourceTraversalItem> items = await CollectAsync(traversal.TraverseAsync(request, null, TestContext.Current.CancellationToken));

		Assert.Single(items);
		Assert.Equal((ulong)data.Length, items[0].Content.Length);
	}

	[Fact]
	public async Task TraverseAsync_RelativePath_Correct()
	{
		Directory.CreateDirectory(Path.Combine(_rootDir, "subdir", "nested"));
		File.WriteAllText(Path.Combine(_rootDir, "subdir", "nested", "deep.txt"), "deep");

		SourceTraversalRequest request = new()
		{
			SourcePath = PathHelper.ToInternalCanonicalUri(_rootDir, BackupSourceType.FileSystem),
			Recursive = true,
		};

		FileSystemTraversal traversal = new();
		List<SourceTraversalItem> items = await CollectAsync(traversal.TraverseAsync(request, null, TestContext.Current.CancellationToken));

		Assert.Single(items);
		Assert.Equal("subdir/nested/deep.txt", items[0].RelativeFilePath);
	}

	private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
	{
		List<T> result = new();
		await foreach(T item in source)
			result.Add(item);
		return result;
	}

	public void Dispose()
	{
		if(Directory.Exists(_rootDir))
			try { Directory.Delete(_rootDir, recursive: true); } catch { }
	}
}
