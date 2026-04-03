using BMTP3.Core2.BackupNew.Engine.Traversal;

namespace BMTP3.Core2.BackupNew.Infrastructure.Traversal;

public class MtpGatekeeper : IMtpGatekeeper, IDisposable
{
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct)
	{
		await _semaphore.WaitAsync(ct);
		try
		{
			return await action();
		} finally
		{
			_semaphore.Release();
		}
	}

	public async Task ExecuteAsync(Func<Task> action, CancellationToken ct)
	{
		await _semaphore.WaitAsync(ct);
		try
		{
			await action();
		} finally
		{
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<IDisposable> AcquireAsync(CancellationToken ct)
	{
		await _semaphore.WaitAsync(ct);
		return new SemaphoreLease(_semaphore);
	}

	public void Dispose()
	{
		_semaphore.Dispose();
	}

	/// <summary>
	/// Holds the semaphore for the duration of a long-running operation (e.g. an MTP stream read).
	/// Releases the semaphore exactly once when disposed.
	/// </summary>
	private sealed class SemaphoreLease : IDisposable
	{
		private readonly SemaphoreSlim _semaphore;
		private int _disposed; // 0 = alive, 1 = disposed  (Interlocked for thread safety)

		internal SemaphoreLease(SemaphoreSlim semaphore) => _semaphore = semaphore;

		public void Dispose()
		{
			if(Interlocked.Exchange(ref _disposed, 1) == 0)
				_semaphore.Release();
		}
	}
}
