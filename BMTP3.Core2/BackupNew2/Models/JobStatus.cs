namespace BMTP3.Core2.BackupNew2.Models;

/// <summary>
/// Defines the overall status of a backup job after execution.
/// </summary>
public enum JobStatus
{
	/// <summary>
	/// The backup job completed successfully without any errors or warnings.
	/// </summary>
	Success,

	/// <summary>
	/// The backup job completed, but encountered some file-specific errors or warnings.
	/// These are typically non-critical and the job as a whole is considered successful.
	/// </summary>
	SuccessWithWarnings,

	/// <summary>
	/// The backup job failed due to a critical error (e.g., source device disconnected, disk full).
	/// </summary>
	Failed,

	Running,
	Completed,
	Cancelled
}
