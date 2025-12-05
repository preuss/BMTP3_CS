using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Errors;

namespace BMTP3.Core2.BackupNew.Models;
/// <summary>
/// The central data object representing a single file throughout the entire backup process.
/// It holds the content, metadata, and state of a single file being processed.
/// The object "travels" through all stages and is continuously enriched with additional metadata.
/// </summary>
public interface IBackupItem
{
	/// <summary>
	/// The physical content source (file system file, MTP object, etc.).
	/// Access to the file's current content (may change from an MTP stream to a local temp file, etc.).
	/// </summary>
	ISourceContent Content { get; }

	/// <summary>
	/// Collection of all metadata – name, path, hashes, timestamps, etc.
	/// Enriched by each stage in the pipeline.
	/// Dynamic metadata container enriched throughout the pipeline.
	/// </summary>
	BackupMetadata Metadata { get; }

	/// <summary>
	/// The current lifecycle state of the item (e.g., New, Analyzed, Completed).
	/// The current state within the backup process.
	/// </summary>
	BackupState State { get; }

	/// <summary>
	/// The decided action to perform (e.g., Copy, Skip).
	/// </summary>
	BackupActionType Action { get; }

	/// <summary>
	/// Collection of all metadata – name, path, hashes, timestamps, etc.
	/// Enriched by each stage in the pipeline.
	/// </summary>
	BackupMetadata BackupMetadata { get; }

	/// <summary>
	/// The current state within the backup process.
	/// </summary>
	BackupProcessState ProcessState { get; }

	/// <summary>
	/// The final outcome of the item's processing (e.g., Completed, Skipped, Failed).
	/// This is set once the item reaches a terminal state.
	/// </summary>
	BackupTerminalState TerminalState { get;  }

	/// <summary>
	/// Error information for this specific item about any error that occurred during processing.
	/// Null if no error has occurred.
	/// Allows a single item to fail iwthou stopping the entire bakcup.
	/// Error information for this specific item.
	/// Allows a single item to fail without stopping the entire backup.
	/// </summary>
	ErrorInfo? ErrorInfo { get; } //TODO: Should this be nullable?

	/// <summary>
	/// Helper method to mark the item as failed.
	/// </summary>
	void Fail(string message, string stepName, System.Exception? ex = null);
}
