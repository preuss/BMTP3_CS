namespace BMTP3.Core3;

/// <summary>
/// Main entry point for the backup engine.
/// Executes a backup plan using a simple, single-threaded, immutable data flow pipeline.
/// </summary>
public interface IBackupEngine
{
	/// <summary>
	/// Execute a backup plan.
	/// Single-threaded sequential pipeline: Scan → Transfer+Sidecar → Metadata → Hash → Timestamp → Result
	/// </summary>
	/// <param name="plan">Backup configuration (source, destination, options).</param>
	/// <param name="progress">Progress reporter. Caller can subscribe to track backup progress.</param>
	/// <param name="ct">Cancellation token to stop the backup.</param>
	/// <returns>Summary of the backup operation (success, items, errors, duration).</returns>
	Task<BackupJobResult> RunAsync(
		BackupPlan plan,
		IProgress<IBackupProgress>? progress,
		CancellationToken ct
	);
}
