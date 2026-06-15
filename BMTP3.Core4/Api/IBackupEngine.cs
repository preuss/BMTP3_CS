using BMTP3.Core4.Api.Models;

namespace BMTP3.Core4.Api;

/// <summary>
/// Defines the entry point for executing a backup job.
/// Implementations are responsible for orchestrating the backup process
/// based on the provided backup plan.
/// </summary>
public interface IBackupEngine
{
	/// <summary>
	/// Executes a backup job using the specified backup plan.
	/// </summary>
	/// <remarks>
	/// Implementations may process files concurrently internally, but progress
	/// reports must be emitted sequentially. Consumers of <paramref name="progress"/>
	/// are not required to be thread-safe.
	///
	/// If <paramref name="progress"/> is provided, all calls to
	/// <see cref="IProgress{T}.Report(T)"/> must happen before the returned task
	/// completes.
	/// </remarks>
	/// <param name="plan">
	/// The immutable configuration describing what should be backed up
	/// and how the backup should behave.
	/// </param>
	/// <param name="progress">
	/// Optional progress reporter that receives snapshot updates during execution.
	/// When provided, it is called sequentially — never concurrently.
	/// </param>
	/// <param name="cancellationToken">
	/// Token used to observe cancellation requests.
	/// </param>
	/// <returns>
	/// A result object describing the final outcome of the backup job.
	/// </returns>
	Task<BackupResult> RunAsync(
		BackupPlan plan,
		IProgress<BackupProgress>? progress,
		CancellationToken cancellationToken
	);
}