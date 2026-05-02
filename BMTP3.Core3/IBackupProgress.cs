namespace BMTP3.Core3;

/// <summary>
/// Progress information reported during backup execution.
/// Allows caller to track which file is being processed and overall progress.
/// </summary>
public interface IBackupProgress
{
	/// <summary>
	/// Name of the file currently being processed.
	/// </summary>
	string CurrentFile { get; }

	/// <summary>
	/// Total bytes processed so far (used for hash/transfer operations).
	/// </summary>
	long BytesTransferred { get; }

	/// <summary>
	/// Number of items processed so far.
	/// </summary>
	int FilesProcessed { get; }

	/// <summary>
	/// Total number of items to process.
	/// </summary>
	int FilesTotal { get; }

	/// <summary>
	/// Current phase of the backup pipeline.
	/// </summary>
	BackupPhase Phase { get; }
}
