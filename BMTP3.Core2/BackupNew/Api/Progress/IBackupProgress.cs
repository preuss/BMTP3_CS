using BMTP3.Core2.BackupNew.Api.Enums;

namespace BMTP3.Core2.BackupNew.Api.Progress;

/// <summary>
///     Represents the current progress of an ongoing backup job.
///     A snapshot of the backup job's current progress.
/// </summary>
public interface IBackupProgress
{
	/// <summary>
	///     The overall phase of the backup job.
	/// </summary>
	BackupPhase Phase { get; }

	/// <summary>
	///     The total number of directories discovered by the scanner so far.
	/// </summary>
	int DirectoriesTraversed { get; }

	/// <summary>
	///     The total number of files discovered by the scanner so far.
	/// </summary>
	int FilesDiscovered { get; }

	/// <summary>
	///     The total size in bytes of all files discovered so far.
	/// </summary>
	long BytesTotal { get; }

	/// <summary>
	///     Number of files processed so far (Success + Skipped + Failed).
	/// </summary>
	int FilesProcessed { get; }

	/// <summary>
	///     The number of files successfully backed up.
	/// </summary>
	int FilesSucceeded { get; }

	/// <summary>
	///     The number of files skipped (e.g., due to existing files or filters).
	/// </summary>
	int FilesSkipped { get; }

	/// <summary>
	///     The number of files that failed processing.
	/// </summary>
	int FilesFailed { get; }

	/// <summary>
	///     The total number of bytes successfully processed (moved/copied/skipped).
	/// </summary>
	long BytesProcessed { get; }

	/// <summary>
	///     Percentage of completion (0..100).
	/// </summary>
	double PercentageComplete { get; }

	/// <summary>
	///     A read-only list of files currently being processed (active in the pipeline).
	/// </summary>
	IReadOnlyList<FileProgress> ActiveFiles { get; }
}