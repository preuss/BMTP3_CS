using BMTP3.Core2.BackupNew.Models;
using BMTP3.Core2.BackupNew.Models.Configuration;

namespace BMTP3.Core2.BackupNew.Engine;

/// <summary>
/// Responsible for discovering files in the source (FileSystem or MTP) 
/// and yielding them as initial IBackupItem objects.
/// Corresponds to Step 1 (Discovery/Preparation) in the specification.
/// </summary>
public interface IBackupScanner
{
	/// <summary>
	/// Scans the source defined in the job and yields items found.
	/// </summary>
	IAsyncEnumerable<IBackupItem> ScanAsync(BackupJob job, CancellationToken ct);
}
