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

	public void Dispose()
	{
		_semaphore.Dispose();
	}
}
