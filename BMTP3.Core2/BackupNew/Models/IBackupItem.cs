using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Errors;

namespace BMTP3.Core2.BackupNew.Models;
/// <summary>
/// The central data object representing a single file throughout the entire backup process.
/// The object "travels" through all stages and is continuously enriched with additional data.
/// </summary>
public interface IBackupItem
{
	/// <summary>
	/// Access to the file's current content (may change from an MTP stream to a local temp file, etc.).
	/// </summary>
	ISourceContent Content { get; }

	/// <summary>
	/// Collection of all metadata – name, path, hashes, timestamps, etc.
	/// Enriched by each stage in the pipeline.
	/// </summary>
	BackupMetadata Metadata { get; }

	/// <summary>
	/// The current state within the backup process.
	/// </summary>
	BackupState State { get; }

	/// <summary>
	/// Error information for this specific item.
	/// Allows a single item to fail without stopping the entire backup.
	/// </summary>
	ErrorInfo ErrorInfo { get; }
}
