using BMTP3.Core4.Traversal;

namespace BMTP3.Core4.Tests.Fakes;

internal sealed class FakeGatekeeper : IMediaDeviceGatekeeper
{
	public IDisposable Lease { get; set; } = new TrackingDisposable();
	public bool AcquireCalled { get; private set; }

	public void Dispose() { }

	public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) => action(ct);

	public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct) => action(ct);

	public Task<IDisposable> AcquireAsync(CancellationToken ct)
	{
		AcquireCalled = true;
		return Task.FromResult(Lease);
	}

	public Task<IDisposable> AcquireAsync(TimeSpan timeout, CancellationToken ct) => AcquireAsync(ct);

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
}

internal sealed class TrackingDisposable : IDisposable
{
	public bool WasDisposed { get; private set; }
	public void Dispose() => WasDisposed = true;
}
