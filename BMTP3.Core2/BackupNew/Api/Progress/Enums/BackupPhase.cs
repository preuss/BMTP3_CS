namespace BMTP3.Core2.BackupNew.Api.Progress.Enums;

/// <summary>
///     The current work phase. Only meaningful when State is Running.
/// </summary>
public enum BackupPhase
{
	/// <summary>
	///     No phase (not started).
	/// </summary>
	None,
	/// <summary>
	///     Initializing the backup job.
	/// </summary>
	Initializing,
	/// <summary>
	///     Traversing source directories/files.
	/// </summary>
	Traversing,
	/// <summary>
	///     Transferring files to destination.
	/// </summary>
	Transferring
}
