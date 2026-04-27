namespace BMTP3.Core2.BackupNew.Api.Progress.Enums;

/// <summary>
///     The current processing phase of the file.
/// </summary>
public enum FilePhase
{
	/// <summary>
	///     No phase (not started).
	/// </summary>
	None,
	/// <summary>
	///     Comparing source and destination files.
	/// </summary>
	Comparing,
	/// <summary>
	///     Copying the file.
	/// </summary>
	Copying
}
