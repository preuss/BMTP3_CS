namespace BMTP3.Core4.Traversal;

/// <summary>
/// Serializes access to an MTP device to ensure single-threaded usage.
/// MTP devices do not support concurrent commands.
/// </summary>
public interface IMediaDeviceGatekeeper : IDisposable
{
	/// <summary>
	/// Acquires exclusive access, executes <paramref name="action"/>,
	/// and releases the lock.
	/// Intended for short, self-contained operations.
	/// </summary>
	Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);

	/// <summary>
	/// Acquires exclusive access, executes <paramref name="action"/>,
	/// and releases the lock.
	/// Intended for short, self-contained operations.
	/// </summary>
	Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct);

	/// <summary>
	/// Acquires exclusive access and returns a lease that holds it until disposed.
	/// Use when access must span multiple operations.
	/// </summary>
	Task<IDisposable> AcquireAsync(CancellationToken ct);

	/// <summary>
	/// Acquires exclusive access with a timeout.
	/// Throws <see cref="TimeoutException"/> if not acquired within the timeout.
	/// </summary>
	Task<IDisposable> AcquireAsync(TimeSpan timeout, CancellationToken ct);

	/// <summary>
	/// Synchronously acquires exclusive access and returns a lease.
	/// </summary>
	IDisposable Acquire(CancellationToken ct);

	/// <summary>
	/// Synchronously acquires exclusive access with a timeout.
	/// Throws <see cref="TimeoutException"/> if not acquired within the timeout.
	/// </summary>
	IDisposable Acquire(TimeSpan timeout, CancellationToken ct);
}