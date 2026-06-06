using BMTP3.Core4.Models;
using BMTP3.Core4.Traversal;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Tests.Traversal;

[SupportedOSPlatform("windows7.0")]
public class MediaDeviceContentTests
{
	[Fact]
	public void Constructor_NullMediaFileInfo_Throws()
	{
		var gatekeeper = new FakeGatekeeper();
		Assert.Throws<ArgumentNullException>(() => new MediaDeviceContent(null!, gatekeeper));
	}

	private sealed class FakeGatekeeper : IMtpGatekeeper
	{
		public IDisposable Lease { get; set; } = new TrackingDisposable();

		public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
			=> throw new NotImplementedException();

		public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
			=> throw new NotImplementedException();

		public Task<IDisposable> AcquireAsync(CancellationToken ct)
		{
			AcquireCalled = true;
			return Task.FromResult(Lease);
		}

		public Task<IDisposable> AcquireAsync(TimeSpan timeout, CancellationToken ct)
			=> AcquireAsync(ct);

		public IDisposable Acquire(CancellationToken ct)
		{
			AcquireCalled = true;
			return Lease;
		}

		public IDisposable Acquire(TimeSpan timeout, CancellationToken ct)
		{
			AcquireCalled = true;
			return Lease;
		}

		public bool AcquireCalled { get; private set; }
	}

	private sealed class TrackingDisposable : IDisposable
	{
		public bool WasDisposed { get; private set; }
		public void Dispose() => WasDisposed = true;
	}
}
