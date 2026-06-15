using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Consoles.Progress;

public static class ProgressReportMapper
{
	public static ProgressReport ToReport(BackupProgress progress)
	{
		ArgumentNullException.ThrowIfNull(progress);

		bool isScanning = progress.CurrentPhase == BackupProgressPhase.Scanning;

		int completed = isScanning
			? 0
			: progress.FilesSucceeded + progress.FilesSkipped + progress.FilesFailed;

		int total = isScanning
			? 1
			: Math.Max(1, progress.TotalFilesSelected);

		string phase = isScanning
			? $"Scanning: {progress.DirectoriesTraversed} dirs, {progress.FilesDiscovered} files"
			: $"Transferring: {completed}/{total} files";

		BackupProgressItem? activeFile = progress.ActiveFiles.Count > 0
			? progress.ActiveFiles[0]
			: null;

		return new ProgressReport(
			completed,
			total,
			progress.DirectoriesTraversed,
			progress.FilesDiscovered,
			phase,
			activeFile?.RelativeFilePath,
			activeFile?.BytesProcessed ?? 0,
			activeFile?.Length ?? 0);
	}
}
