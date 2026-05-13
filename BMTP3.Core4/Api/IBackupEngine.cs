using BMTP3.Core4.Models;

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
	/// <param name="plan">
	/// The immutable configuration describing what should be backed up
	/// and how the backup should behave.
	/// </param>
	/// <param name="progress">
	/// Optional progress reporter that receives snapshot updates
	/// during execution.
	/// </param>
	/// <param name="cancellationToken">
	/// Token used to observe cancellation requests.
	/// </param>
	/// <returns>
	/// A result object describing the final outcome of the backup job.
	/// </returns>
	Task<BackupJobResult> RunAsync(
		BackupPlan plan,
		IProgress<IBackupProgress>? progress,
		CancellationToken cancellationToken);
}