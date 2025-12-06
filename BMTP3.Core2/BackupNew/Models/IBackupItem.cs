using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Errors;
using BMTP3.Core2.BackupNew.Job;

namespace BMTP3.Core2.BackupNew.Models;
/// <summary>
/// The central data object representing a single file throughout the backup process.
/// Holds the content, metadata, lifecycle state, and result state of a single file.
/// </summary>
public interface IBackupItem
{
	/// <summary>
	/// The physical content source (file system file, MTP object, etc.).
	/// May change from an MTP stream to a local temp file during processing.
	/// </summary>
	ISourceContent Content { get; }

	/// <summary>
	/// Dynamic metadata container enriched throughout the pipeline.
	/// Contains name, path, hashes, timestamps, etc.
	/// </summary>
	BackupMetadata Metadata { get; }

	/// <summary>
	/// The current lifecycle state of the item (e.g., New, Queued, Active, Processed).
	/// </summary>
	LifecycleState LifecycleState { get; }

	/// <summary>
	/// The final outcome of the backup attempt (e.g., Pending, Success, Failed, Skipped).
	/// </summary>
	ResultState ResultState { get; }

	/// <summary>
	/// Error information for this specific item if an error occurred during processing.
	/// Null if no error has occurred.
	/// </summary>
	ErrorLog Errors { get; }

	/// <summary>
	/// The audit trail of the item, containing logs of all significant events.
	/// </summary>
	IReadOnlyList<AuditEntry> AuditTrail { get; }

	/// <summary>
	/// The number of attempts made to process the item.
	///	</summary>
	uint AttemptCount { get; }


	/// <summary>
	/// Helper method to mark the item as failed.
	/// </summary>
	void Fail(string message, string stepName, Exception? ex = null);

	/// <summary>
	/// Replaces the current content source (e.g., when copying from MTP to local temp).
	/// </summary>
	void ReplaceContent(ISourceContent newContent);
}
