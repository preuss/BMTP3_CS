using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Models;
using BMTP3.Core4.Scanner;
using BMTP3.Core4.Tests.Fakes;
using BMTP3.Core4.Traversal;

namespace BMTP3.Core4.Tests.Scanner;

public class BackupScannerTests
{
	[Fact]
	public async Task ScanAsync_MapsTraversalItemsToBackupItems()
	{
		FakeSourceTraversal traversal = new(CreateTraversalItems(3));

		BackupScanRequest request = new()
		{
			SourcePath = "C:\\Photos",
			Recursive = true,
			ItemIdScope = ItemIdScope.Session,
		};

		BackupScanner scanner = new();
		List<BackupItem> items = await CollectAsync(scanner.ScanAsync(traversal, request, null, TestContext.Current.CancellationToken));

		Assert.Equal(3, items.Count);
		Assert.Equal("item0", items[0].Id);
		Assert.Equal("C:\\Photos\\file0.jpg", items[0].SourcePath);
		Assert.Equal("file0.jpg", items[0].FileName);
		Assert.Equal("file0.jpg", items[0].RelativeFilePath);
	}

	[Fact]
	public async Task ScanAsync_WithProgress_ReportsProgress()
	{
		FakeSourceTraversal traversal = new(CreateTraversalItems(3));

		BackupScanRequest request = new()
		{
			SourcePath = "C:\\Photos",
			Recursive = true,
		};

		CountingProgress<BackupScanProgress> progress = new();

		BackupScanner scanner = new();

		await CollectAsync(scanner.ScanAsync(
			traversal,
			request,
			progress,
			TestContext.Current.CancellationToken));

		Assert.Equal(3, progress.Count);
	}

	private sealed class CountingProgress<T> : IProgress<T>
	{
		public int Count { get; private set; }

		public void Report(T value)
		{
			Count++;
		}
	}

	[Fact]
	public async Task ScanAsync_Cancelled_Throws()
	{
		FakeSourceTraversal traversal = new(CreateTraversalItems(5));
		using CancellationTokenSource cts = new();
		cts.Cancel();

		BackupScanRequest request = new()
		{
			SourcePath = "C:\\Photos",
			Recursive = true,
		};

		BackupScanner scanner = new();
		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			CollectAsync(scanner.ScanAsync(traversal, request, null, cts.Token)));
	}

	[Fact]
	public async Task ScanAsync_NullTraversal_Throws()
	{
		BackupScanner scanner = new();
		BackupScanRequest request = new() { SourcePath = "C:\\" };

		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			CollectAsync(scanner.ScanAsync(null!, request, null, TestContext.Current.CancellationToken)));
	}

	[Fact]
	public async Task ScanAsync_NullRequest_Throws()
	{
		FakeSourceTraversal traversal = new(Array.Empty<SourceTraversalItem>());
		BackupScanner scanner = new();

		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			CollectAsync(scanner.ScanAsync(traversal, null!, null, TestContext.Current.CancellationToken)));
	}

	[Fact]
	public async Task ScanAsync_EmptyTraversal_ReturnsEmpty()
	{
		FakeSourceTraversal traversal = new(Array.Empty<SourceTraversalItem>());
		BackupScanRequest request = new() { SourcePath = "C:\\" };

		BackupScanner scanner = new();
		List<BackupItem> items = await CollectAsync(scanner.ScanAsync(traversal, request, null, TestContext.Current.CancellationToken));

		Assert.Empty(items);
	}

	[Fact]
	public async Task ScanAsync_SubPath_PassedToTraversal()
	{
		FakeSourceTraversal traversal = new(CreateTraversalItems(1));
		BackupScanRequest request = new()
		{
			SourcePath = "C:\\Photos",
			Recursive = true,
		};

		BackupScanner scanner = new();
		await CollectAsync(scanner.ScanAsync(traversal, request, null, TestContext.Current.CancellationToken));

		Assert.Equal("C:\\Photos", traversal.LastRequest?.SourcePath);
		Assert.True(traversal.LastRequest?.Recursive);
	}

	[Fact]
	public async Task ScanAsync_ItemIdScope_PassedToTraversal()
	{
		FakeSourceTraversal traversal = new(CreateTraversalItems(1));
		BackupScanRequest request = new()
		{
			SourcePath = "C:\\Photos",
			ItemIdScope = ItemIdScope.Persistent,
		};

		BackupScanner scanner = new();
		await CollectAsync(scanner.ScanAsync(traversal, request, null, TestContext.Current.CancellationToken));

		Assert.Equal(ItemIdScope.Persistent, traversal.LastRequest?.ItemIdScope);
	}

	private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
	{
		List<T> result = new();
		await foreach(T item in source)
			result.Add(item);
		return result;
	}

	private static SourceTraversalItem[] CreateTraversalItems(int count)
	{
		return Enumerable.Range(0, count).Select(i => new SourceTraversalItem
		{
			Id = $"item{i}",
			SourcePath = $"C:\\Photos\\file{i}.jpg",
			RelativeFilePath = $"file{i}.jpg",
			FileName = $"file{i}.jpg",
			Content = new FakeContent(new byte[] { (byte)i }),
		}).ToArray();
	}

	internal sealed class FakeSourceTraversal : ISourceTraversal
	{
		private readonly IReadOnlyList<SourceTraversalItem> _items;
		public SourceTraversalRequest? LastRequest { get; private set; }

		public FakeSourceTraversal(IReadOnlyList<SourceTraversalItem> items)
		{
			_items = items;
		}

		public async IAsyncEnumerable<SourceTraversalItem> TraverseAsync(
			SourceTraversalRequest request,
			IProgress<SourceTraversalProgress>? progress,
			[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
		{
			LastRequest = request;
			foreach(SourceTraversalItem item in _items)
			{
				cancellationToken.ThrowIfCancellationRequested();
				progress?.Report(new SourceTraversalProgress { DirectoriesTraversed = 1, FilesDiscovered = 1 });
				yield return item;

				// Add a minimal await to satisfy CS1998
				await Task.Yield();
			}
		}
	}
}
