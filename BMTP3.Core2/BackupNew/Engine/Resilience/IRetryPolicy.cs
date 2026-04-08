namespace BMTP3.Core2.BackupNew.Engine.Resilience;

/// <summary>
///     Strategy interface for retry policies.
///     Allows pluggable retry logic (exponential backoff, fixed intervals, etc.).
/// </summary>
public interface IRetryPolicy
{
	/// <summary>
	///     Executes an async action with retry logic.
	/// </summary>
	/// <typeparam name="T">Return type of the action.</typeparam>
	/// <param name="action">The action to execute.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Result of the action if successful.</returns>
	/// <exception cref="OperationCanceledException">If cancellation is requested.</exception>
	/// <exception cref="Exception">If all retries are exhausted.</exception>
	Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct);
}