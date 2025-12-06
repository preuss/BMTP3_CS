using BMTP3.Core2.BackupNew.Engine;

namespace BMTP3.Core2.BackupNew.Api;

/// <summary>
/// Defines the contract for the main backup engine, orchestrating the backup process for a single job.
/// </summary>
public interface IBackupEngine
{
	/// <summary>
	/// Executes a single backup job asynchronously.
	/// </summary>
	/// <param name="job">The configuration for the backup job.</param>
	/// <param name="progress">An object to report progress updates during the job execution.</param>
	/// <param name="ct">A CancellationToken to observe for cancellation requests.</param>
	/// <returns>A BackupJobResult object summarizing the outcome of the job.</returns>
	Task<BackupJobResult> RunAsync(BackupPlan job, IProgress<BackupProgress> progress, CancellationToken ct);
}
