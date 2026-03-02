using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Resilience;

/// <summary>
/// Retry policy with exponential backoff.
/// Configurable via max attempts and base backoff duration.
/// </summary>
public class ExponentialBackoffRetryPolicy : IRetryPolicy
{
	private readonly int _maxAttempts;
	private readonly int _baseBackoffMs;
	private readonly ILogger<ExponentialBackoffRetryPolicy>? _logger;

	public ExponentialBackoffRetryPolicy(int maxAttempts, int baseBackoffMs, ILogger<ExponentialBackoffRetryPolicy>? logger = null)
	{
		if(maxAttempts < 1)
			throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be at least 1.");
		if(baseBackoffMs < 0)
			throw new ArgumentOutOfRangeException(nameof(baseBackoffMs), "Base backoff must be non-negative.");

		_maxAttempts = maxAttempts;
		_baseBackoffMs = baseBackoffMs;
		_logger = logger;
	}

	public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct)
	{
		int attempt = 0;
		Exception? lastException = null;

		while(attempt < _maxAttempts)
		{
			attempt++;
			ct.ThrowIfCancellationRequested();

			try
			{
				_logger?.LogDebug("Retry attempt {Attempt}/{MaxAttempts}", attempt, _maxAttempts);
				return await action();
			} catch(OperationCanceledException)
			{
				throw;
			} catch(Exception ex)
			{
				lastException = ex;
				_logger?.LogWarning(ex, "Attempt {Attempt}/{MaxAttempts} failed", attempt, _maxAttempts);

				if(attempt >= _maxAttempts)
				{
					_logger?.LogError(ex, "All {MaxAttempts} retry attempts exhausted", _maxAttempts);
					throw;
				}

				int backoffMs = _baseBackoffMs * (1 << (attempt - 1)); // Exponential: base * 2^(attempt-1)
				_logger?.LogDebug("Backing off for {BackoffMs}ms before retry", backoffMs);
				await Task.Delay(backoffMs, ct);
			}
		}

		throw lastException ?? new InvalidOperationException("Retry logic failed without capturing an exception.");
	}
}
