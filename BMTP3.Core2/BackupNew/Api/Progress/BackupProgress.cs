using BMTP3.Core2.BackupNew.Api.Enums;
using System.Collections.Generic;

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
	public BackupPhase Phase { get; set; }

	/// <summary>
	/// The total number of directories discovered by the scanner so far.
	/// </summary>
	public int DirectoriesTraversed { get; set; }

	/// <summary>
	/// The total number of files discovered by the scanner so far.
	/// </summary>
	public int FilesDiscovered { get; set; }

	/// <summary>
	/// The total size in bytes of all files discovered so far.
	/// </summary>
	public long BytesTotal { get; set; }

	/// <summary>
	/// Number of files processed so far.
	/// The total number of files that have finished processing (Success + Skipped + Failed).
	/// </summary>
	public int FilesProcessed { get; set; }

	/// <summary>
	/// The number of files successfully backed up.
	/// </summary>
	public int FilesSucceeded { get; set; }

	/// <summary>
	/// The number of files skipped (e.g., due to existing files or filters).
	/// </summary>
	public int FilesSkipped { get; set; }

	/// <summary>
	/// The number of files that failed processing.
	/// </summary>
	public int FilesFailed { get; set; }

	/// <summary>
	/// The total number of bytes successfully processed (moved/copied/skipped).
	/// </summary>
	public long BytesProcessed { get; set; }

	/// <summary>
	/// Percentage of completion.
	/// </summary>
	public double PercentageComplete => FilesDiscovered > 0 ? (double)FilesProcessed / FilesDiscovered * 100 : 0;

	// Internal mutable backing list used during the job.
	private List<FileProgress> _activeFiles = new();

	/// <summary>
	/// A read-only list of files currently being processed (active in the pipeline).
	/// Exposed as IReadOnlyList to signal snapshot semantics.
	/// </summary>
	public IReadOnlyList<FileProgress> ActiveFiles => _activeFiles.AsReadOnly();

	/// <summary>
	/// Helper for engine to add active file entries while building the snapshot.
	/// This keeps the public surface read-only.
	/// </summary>
	public void AddActiveFile(FileProgress fp) => _activeFiles.Add(fp);

	/// <summary>
	/// Helper to clear the active list when appropriate.
	/// </summary>
	public void ClearActiveFiles() => _activeFiles.Clear();
}
