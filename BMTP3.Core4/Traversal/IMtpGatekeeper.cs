namespace BMTP3.Core4.Traversal;

/// <summary>
/// Controls access to an MTP device to ensure single-threaded usage.
/// MTP devices do not support concurrent commands.
/// </summary>
public interface IMtpGatekeeper
{
	/// <summary>
	/// Acquires the exclusive lock, executes <paramref name="action"/>,
	/// and then releases the lock.
	/// Use for short self-contained operations such as directory enumeration
	/// or metadata reads.
	/// </summary>
	Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);

	/// <summary>
	/// Acquires the exclusive lock, executes <paramref name="action"/>,
	/// and then releases the lock.
	/// </summary>
	Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct);

	/// <summary>
	/// Acquires the exclusive lock and returns a lease that holds it until disposed.
	/// Use when exclusive access must span multiple operations, such as opening a stream
	/// and reading all its bytes, to avoid per-chunk acquire/release overhead.
	/// The caller must dispose the returned lease.
	/// </summary>
	Task<IDisposable> AcquireAsync(CancellationToken ct);

	/// <summary>
	/// Acquires the exclusive lock with a specific timeout.
	/// The timeout applies only while waiting to acquire the lock.
	/// It does not limit the duration of work performed while holding the lease.
	/// Throws <see cref="TimeoutException"/> if the lock is not acquired within the timeout.
	/// </summary>
	Task<IDisposable> AcquireAsync(TimeSpan timeout, CancellationToken ct);
}
