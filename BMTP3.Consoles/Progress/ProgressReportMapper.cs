using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Consoles.Progress;

internal static class ProgressReportMapper
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

		BackupProgressItem? activeFile = progress.ActiveFiles.Count > 0
			? progress.ActiveFiles[0]
			: null;

		string phase = isScanning
			? $"Scanning: {progress.DirectoriesTraversed} dirs, {progress.FilesDiscovered} files"
			: activeFile != null
				? $"{activeFile.Phase}: {activeFile.RelativeFilePath} ({completed}/{total} files)"
				: $"Transferring: {completed}/{total} files";

		return new ProgressReport(
			completed,
			total,
			progress.DirectoriesTraversed,
			progress.FilesDiscovered,
			phase,
			activeFile?.RelativeFilePath,
			activeFile?.Phase.ToString(),
			activeFile?.BytesProcessed ?? 0,
			activeFile?.Length ?? 0,
			progress.BytesProcessed,
			progress.TotalBytesSelected);
	}
}