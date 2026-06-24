namespace BMTP3.Core4.Traversal;

public sealed class MediaDeviceGatekeeper : IMediaDeviceGatekeeper
{
	private readonly SemaphoreSlim _semaphore = new(1, 1);
	private int _disposed;

	public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(action);
		ThrowIfDisposed();

		using IDisposable lease = await AcquireAsync(ct).ConfigureAwait(false);
		return await action(ct).ConfigureAwait(false);
	}

	public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(action);
		ThrowIfDisposed();

		using IDisposable lease = await AcquireAsync(ct).ConfigureAwait(false);
		await action(ct).ConfigureAwait(false);
	}

	public async Task<IDisposable> AcquireAsync(CancellationToken ct)
	{
		ThrowIfDisposed();

		await _semaphore.WaitAsync(ct).ConfigureAwait(false);

		if (Volatile.Read(ref _disposed) != 0)
		{
			_semaphore.Release();
			throw new ObjectDisposedException(nameof(MediaDeviceGatekeeper));
		}

		return new SemaphoreLease(_semaphore);
	}

	public async Task<IDisposable> AcquireAsync(TimeSpan timeout, CancellationToken ct)
	{
		ThrowIfDisposed();

		bool entered = await _semaphore.WaitAsync(timeout, ct).ConfigureAwait(false);
		if (!entered)
		{
			throw new TimeoutException("Timed out waiting to acquire the MTP gatekeeper.");
		}

		if (Volatile.Read(ref _disposed) != 0)
		{
			_semaphore.Release();
			throw new ObjectDisposedException(nameof(MediaDeviceGatekeeper));
		}

		return new SemaphoreLease(_semaphore);
	}

	public IDisposable Acquire(CancellationToken ct)
	{
		ThrowIfDisposed();

		_semaphore.Wait(ct);

		if (Volatile.Read(ref _disposed) != 0)
		{
			_semaphore.Release();
			throw new ObjectDisposedException(nameof(MediaDeviceGatekeeper));
		}

		return new SemaphoreLease(_semaphore);
	}

	public IDisposable Acquire(TimeSpan timeout, CancellationToken ct)
	{
		ThrowIfDisposed();

		bool entered = _semaphore.Wait(timeout, ct);
		if (!entered)
		{
			throw new TimeoutException("Timed out waiting to acquire the MTP gatekeeper.");
		}

		if (Volatile.Read(ref _disposed) != 0)
		{
			_semaphore.Release();
			throw new ObjectDisposedException(nameof(MediaDeviceGatekeeper));
		}

		return new SemaphoreLease(_semaphore);
	}

	public void Dispose()
	{
		// Logical disposal only:
		// prevents new acquisitions but allows existing leases to release safely.
		Interlocked.Exchange(ref _disposed, 1);
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
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
			if (Interlocked.Exchange(ref _disposed, 1) == 0)
			{
				_semaphore.Release();
			}
		}
	}
}