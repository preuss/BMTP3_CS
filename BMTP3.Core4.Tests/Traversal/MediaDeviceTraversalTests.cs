using BMTP3.Core4.Traversal;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Tests.Traversal;

[SupportedOSPlatform("windows7.0")]
public class MediaDeviceTraversalTests
{
	[Fact]
	public void Constructor_NullDevice_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new MediaDeviceTraversal(null!, new FakeGatekeeper()));
	}

	private sealed class FakeGatekeeper : IMediaDeviceGatekeeper
	{
		public IDisposable Lease { get; set; } = new TrackingDisposable();

		public void Dispose() { }

		public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
			=> action(ct);

		public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
			=> action(ct);

		public Task<IDisposable> AcquireAsync(CancellationToken ct)
			=> Task.FromResult(Lease);

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
}
