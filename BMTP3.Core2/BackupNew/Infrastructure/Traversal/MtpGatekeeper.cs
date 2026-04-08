using BMTP3.Core2.BackupNew.Engine.Traversal;

namespace BMTP3.Core2.BackupNew.Infrastructure.Traversal;

public class MtpGatekeeper : IMtpGatekeeper, IDisposable
{
	private readonly int _defaultTimeoutMs;
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public MtpGatekeeper(int defaultTimeoutMs = 60000)
	{
		_defaultTimeoutMs = defaultTimeoutMs > 0 ? defaultTimeoutMs : 60000;
	}

	public void Dispose()
	{
		_semaphore.Dispose();
	}

	public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct)
	{
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(_defaultTimeoutMs);
		await _semaphore.WaitAsync(cts.Token);
		try
		{
			return await action();
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task ExecuteAsync(Func<Task> action, CancellationToken ct)
	{
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(_defaultTimeoutMs);
		await _semaphore.WaitAsync(cts.Token);
		try
		{
			await action();
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<IDisposable> AcquireAsync(CancellationToken ct)
	{
		return await AcquireAsync(_defaultTimeoutMs, ct);
	}

	/// <summary>
	///     Acquires the semaphore with a specific timeout.
	/// </summary>
	public async Task<IDisposable> AcquireAsync(int timeoutMs, CancellationToken ct)
	{
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(timeoutMs > 0 ? timeoutMs : _defaultTimeoutMs);
		await _semaphore.WaitAsync(cts.Token);
		return new SemaphoreLease(_semaphore);
	}

	/// <summary>
	///     Holds the semaphore for the duration of a long-running operation (e.g. an MTP stream read).
	///     Releases the semaphore exactly once when disposed.
	/// </summary>
	private sealed class SemaphoreLease : IDisposable
	{
		private readonly SemaphoreSlim _semaphore;
		private int _disposed; // 0 = alive, 1 = disposed  (Interlocked for thread safety)

		internal SemaphoreLease(SemaphoreSlim semaphore)
		{
			_semaphore = semaphore;
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) == 0)
			{
				_semaphore.Release();
			}
		}
	}
}