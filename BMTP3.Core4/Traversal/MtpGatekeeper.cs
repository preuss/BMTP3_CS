namespace BMTP3.Core4.Traversal;

public sealed class MtpGatekeeper : IMtpGatekeeper, IDisposable
{
	private readonly SemaphoreSlim _semaphore = new(1, 1);
	private int _disposed;

	public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(action);
		ThrowIfDisposed();

		using IDisposable lease = await AcquireAsync(ct);
		return await action(ct);
	}

	public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(action);
		ThrowIfDisposed();

		using IDisposable lease = await AcquireAsync(ct);
		await action(ct);
	}

	public async Task<IDisposable> AcquireAsync(CancellationToken ct)
	{
		ThrowIfDisposed();

		await _semaphore.WaitAsync(ct);
		return new SemaphoreLease(_semaphore);
	}

	// Optional: opt-in timeout only when explicitly requested
	public async Task<IDisposable> AcquireAsync(TimeSpan timeout, CancellationToken ct)
	{
		ThrowIfDisposed();

		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(timeout);

		try
		{
			await _semaphore.WaitAsync(cts.Token);
		} catch(OperationCanceledException) when(!ct.IsCancellationRequested)
		{
			throw new TimeoutException("Timed out waiting to acquire MTP gatekeeper.");
		}

		return new SemaphoreLease(_semaphore);
	}

	public void Dispose()
	{
		if(Interlocked.Exchange(ref _disposed, 1) == 0)
		{
			_semaphore.Dispose();
		}
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_disposed != 0, this);
	}

	private sealed class SemaphoreLease : IDisposable
	{
		private readonly SemaphoreSlim _semaphore;
		private int _disposed;

		internal SemaphoreLease(SemaphoreSlim semaphore)
		{
			_semaphore = semaphore;
		}

		public void Dispose()
		{
			if(Interlocked.Exchange(ref _disposed, 1) == 0)
			{
				_semaphore.Release();
			}
		}
	}
}
