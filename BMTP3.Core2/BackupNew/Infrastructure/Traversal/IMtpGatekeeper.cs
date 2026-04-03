namespace BMTP3.Core2.BackupNew.Engine.Traversal;

/// <summary>
/// Controls access to the MTP device to ensure single-threaded usage.
/// MTP devices do not support concurrent commands.
/// </summary>
public interface IMtpGatekeeper
{
	/// <summary>
	/// Executes an async action exclusively on the MTP device.
	/// The semaphore is acquired before calling <paramref name="action"/> and released
	/// immediately after it completes. Use this for short, self-contained operations
	/// (e.g. directory enumeration, metadata reads).
	/// </summary>
	Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct);

	/// <summary>
	/// Executes an async action exclusively on the MTP device.
	/// </summary>
	Task ExecuteAsync(Func<Task> action, CancellationToken ct);

	/// <summary>
	/// Acquires the MTP semaphore and returns a lease that holds it until disposed.
	/// Use this when exclusive access must span multiple operations (e.g. opening a
	/// stream and reading all its bytes), so the semaphore is acquired once rather
	/// than once per read chunk.
	/// </summary>
	/// <remarks>
	/// The caller MUST dispose the returned lease; failure to do so will permanently
	/// block all other MTP operations.
	/// </remarks>
	Task<IDisposable> AcquireAsync(CancellationToken ct);
}
