namespace BMTP3.Core2.BackupNew.Api.Enums;

/// <summary>
///     Represents the high-level lifecycle phases of the entire backup job.
/// </summary>
public enum BackupPhase
{
	/// <summary>
	///     The job is initializing and preparing to start.
	/// </summary>
	Starting,

	/// <summary>
	///     The scanner is actively running, discovering new files and directories.
	///     Backup processing occurs concurrently during this phase.
	/// </summary>
	Traversing,

	/// <summary>
	///     Scanning has finished. The engine is processing the remaining items in the pipeline.
	/// </summary>
	Transferring,

	/// <summary>
	///     The job has completed successfully.
	/// </summary>
	Completed,

	/// <summary>
	///     The job was cancelled by the user or a system signal.
	/// </summary>
	Cancelled
}