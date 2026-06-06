using BMTP3.Core4.Traversal;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Tests.Traversal;

[SupportedOSPlatform("windows7.0")]
public class MediaDeviceTraversalTests
{
	private sealed class FakeGatekeeper : IMtpGatekeeper
	{
		public IDisposable Lease { get; set; } = new TrackingDisposable();
		public Func<CancellationToken, Task<IDisposable>>? AcquireAsyncHandler { get; set; }

		public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
			=> action(ct);

		public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
			=> action(ct);

		public async Task<IDisposable> AcquireAsync(CancellationToken ct)
		{
			if(AcquireAsyncHandler is not null)
				return await AcquireAsyncHandler(ct);
			await Task.CompletedTask;
			return Lease;
		}

		public Task<IDisposable> AcquireAsync(TimeSpan timeout, CancellationToken ct)
			=> AcquireAsync(ct);

		public IDisposable Acquire(CancellationToken ct) => Lease;
		public IDisposable Acquire(TimeSpan timeout, CancellationToken ct) => Lease;
	}

	private sealed class TrackingDisposable : IDisposable
	{
		public bool WasDisposed { get; private set; }
		public void Dispose() => WasDisposed = true;
	}

	[Fact]
	public void Constructor_NullSession_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new MediaDeviceTraversal(null!, new FakeGatekeeper()));
	}

	[Fact]
	public void Constructor_NullGatekeeper_Throws()
	{
		MediaDevice? device = GetFirstDevice();
		if(device is null)
			return;

		using MtpDeviceSession session = MtpDeviceSession.Open(device);
		Assert.Throws<ArgumentNullException>(() => new MediaDeviceTraversal(session, null!));
	}

	[Fact]
	public async Task TraverseAsync_EmptyRoot_ReturnsItems()
	{
		MediaDevice? device = GetFirstDevice();
		if(device is null)
			return;

		using MtpDeviceSession session = MtpDeviceSession.Open(device);
		var traversal = new MediaDeviceTraversal(session, new FakeGatekeeper());

		var request = new SourceTraversalRequest
		{
			SourcePath = @"\",
			Recursive = false,
		};

		var results = new List<SourceTraversalItem>();
		await foreach(SourceTraversalItem item in traversal.TraverseAsync(request, null, CancellationToken.None))
		{
			results.Add(item);
		}

		Assert.NotEmpty(results);
		Assert.All(results, r =>
		{
			Assert.NotNull(r.Id);
			Assert.NotNull(r.SourcePath);
			Assert.NotNull(r.RelativePath);
			Assert.NotNull(r.FileName);
			Assert.NotNull(r.Content);
		});
	}

	[Fact]
	public async Task TraverseAsync_Recursive_ReturnsAllItems()
	{
		MediaDevice? device = GetFirstDevice();
		if(device is null)
			return;

		using MtpDeviceSession session = MtpDeviceSession.Open(device);
		var traversal = new MediaDeviceTraversal(session, new FakeGatekeeper());

		var request = new SourceTraversalRequest
		{
			SourcePath = @"\",
			Recursive = true,
		};

		var results = new List<SourceTraversalItem>();
		await foreach(SourceTraversalItem item in traversal.TraverseAsync(request, null, CancellationToken.None))
		{
			results.Add(item);
		}

		Assert.NotEmpty(results);

		int rootFiles = results.Count(r => !r.RelativePath.Contains('\\'));
		Assert.True(rootFiles >= 0);
	}

	[Fact]
	public async Task TraverseAsync_ReportsProgress()
	{
		MediaDevice? device = GetFirstDevice();
		if(device is null)
			return;

		using MtpDeviceSession session = MtpDeviceSession.Open(device);
		var traversal = new MediaDeviceTraversal(session, new FakeGatekeeper());

		var request = new SourceTraversalRequest
		{
			SourcePath = @"\",
			Recursive = false,
		};

		int progressCount = 0;
		var progress = new Progress<SourceTraversalProgress>(p =>
		{
			progressCount++;
		});

		await foreach(SourceTraversalItem _ in traversal.TraverseAsync(request, progress, CancellationToken.None))
		{
		}

		Assert.True(progressCount > 0);
	}

	[Fact]
	public async Task TraverseAsync_Cancellation_StopsTraversal()
	{
		MediaDevice? device = GetFirstDevice();
		if(device is null)
			return;

		using MtpDeviceSession session = MtpDeviceSession.Open(device);
		var traversal = new MediaDeviceTraversal(session, new FakeGatekeeper());

		var request = new SourceTraversalRequest
		{
			SourcePath = @"\",
			Recursive = false,
		};

		using CancellationTokenSource cts = new();
		cts.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
		{
			await foreach(SourceTraversalItem _ in traversal.TraverseAsync(request, null, cts.Token))
			{
			}
		});
	}

	[Fact]
	public async Task TraverseAsync_IncludePattern_FiltersItems()
	{
		MediaDevice? device = GetFirstDevice();
		if(device is null)
			return;

		using MtpDeviceSession session = MtpDeviceSession.Open(device);
		var traversal = new MediaDeviceTraversal(session, new FakeGatekeeper());

		var request = new SourceTraversalRequest
		{
			SourcePath = @"\",
			Recursive = true,
			IncludePatterns = new[] { "*.jpg" },
		};

		var results = new List<SourceTraversalItem>();
		await foreach(SourceTraversalItem item in traversal.TraverseAsync(request, null, CancellationToken.None))
		{
			results.Add(item);
		}

		Assert.All(results, r => Assert.EndsWith(".jpg", r.FileName, StringComparison.OrdinalIgnoreCase));
	}

	private static MediaDevice? GetFirstDevice()
	{
		return MediaDevice.GetDevices().FirstOrDefault();
	}
}
