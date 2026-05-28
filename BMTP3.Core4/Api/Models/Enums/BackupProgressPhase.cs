namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Represents the high-level lifecycle phases of a backup job.
/// These phases are coarse-grained and intended for progress reporting
/// and user-facing status display.
/// </summary>
public enum BackupProgressPhase
{
	/// <summary>
	/// The backup job is initializing and preparing to start.
	/// </summary>
	Starting,

	/// <summary>
	/// The source is being scanned and items are being discovered.
	/// </summary>
	Scanning,

	/// <summary>
	/// Discovered items are being processed and transferred
	/// to the destination.
	/// </summary>
	Transferring,

	/// <summary>
	/// The backup job has completed successfully.
	/// </summary>
	Completed,
}