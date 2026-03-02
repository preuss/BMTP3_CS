namespace BMTP3.Core2.BackupNew.Infrastructure.Traversal;

/// <summary>
/// Controls access to the MTP device to ensure single-threaded usage.
/// MTP devices do not support concurrent commands.
/// </summary>
public interface IMtpGatekeeper
{
	/// <summary>
	/// Executes an async action exclusively on the MTP device.
	/// </summary>
	Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct);

	/// <summary>
	/// Executes an async action exclusively on the MTP device.
	/// </summary>
	Task ExecuteAsync(Func<Task> action, CancellationToken ct);
}
