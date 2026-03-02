using BMTP3.Core2.BackupNew.Api.Enums;

namespace BMTP3.Core2.BackupNew.Api.Progress;

/// <summary>
/// Represents the current progress of an ongoing backup job.
/// A snapshot of the backup job's current progress.
/// </summary>
public class BackupProgress : IBackupProgress
{
	/// <summary>
	/// The overall phase of the backup job.
	/// </summary>
	public BackupPhase Phase { get; init; }

	/// <summary>
	/// The total number of directories discovered by the scanner so far.
	/// </summary>
	public int DirectoriesTraversed { get; init; }

	/// <summary>
	/// The total number of files discovered by the scanner so far.
	/// </summary>
	public int FilesDiscovered { get; init; }

	/// <summary>
	/// The total size in bytes of all files discovered so far.
	/// </summary>
	public long BytesTotal { get; init; }

	/// <summary>
	/// Number of files processed so far.
	/// The total number of files that have finished processing (Success + Skipped + Failed).
	/// </summary>
	public int FilesProcessed { get; init; }

	/// <summary>
	/// The number of files successfully backed up.
	/// </summary>
	public int FilesSucceeded { get; init; }

	/// <summary>
	/// The number of files skipped (e.g., due to existing files or filters).
	/// </summary>
	public int FilesSkipped { get; init; }

	/// <summary>
	/// The number of files that failed processing.
	/// </summary>
	public int FilesFailed { get; init; }

	/// <summary>
	/// The total number of bytes successfully processed (moved/copied/skipped).
	/// </summary>
	public long BytesProcessed { get; init; }

	/// <summary>
	/// Percentage of completion.
	/// </summary>
	public double PercentageComplete => FilesDiscovered > 0 ? (double)FilesProcessed / FilesDiscovered * 100 : 0;

	/// <summary>
	/// A list of files currently being processed (active in the pipeline).
	/// </summary>
	public required List<FileProgress> ActiveFiles { get; init; }
}
