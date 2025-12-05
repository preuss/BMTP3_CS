using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Errors;

namespace BMTP3.Core2.BackupNew.Models;

/// <summary>
/// Concrete implementation of IBackupItem.
/// Fully encapsulated, immutable from the outside except through explicit methods.
/// </summary>
public class BackupItem : IBackupItem
{
	public ISourceContent Content { get; private set; }
	public BackupMetadata Metadata { get; private set; }
	public BackupState State { get; private set; }
	public BackupActionType Action { get; private set; }
	/// <summary>
	/// Collection of all metadata – name, path, hashes, timestamps, etc.
	/// Enriched by each stage in the pipeline.
	/// </summary>
	public BackupMetadata BackupMetadata { get; private set; }

	public BackupProcessState ProcessState { get; private set; }
	/// <summary>
	/// The final outcome of the item's processing (e.g., Completed, Skipped, Failed).
	/// This is set once the item reaches a terminal state.
	/// </summary>
	public BackupTerminalState TerminalState { get; private set; }
	//TODO: Should this be nullable?
	public ErrorInfo? ErrorInfo { get; private set; }

	// Private constructor – all object state is initialized here
	private BackupItem(ISourceContent content, BackupMetadata metadata)
	{
		ArgumentNullException.ThrowIfNull(content);
		ArgumentNullException.ThrowIfNull(metadata);
		Content = content;
		Metadata = metadata;
		State = BackupState.Pending;
		Action = BackupActionType.Unknown;
		BackupMetadata = metadata;
		ProcessState = BackupProcessState.New;
		TerminalState = BackupTerminalState.None;
		ErrorInfo = new ErrorInfo();
	}

	/// <summary>
	/// Creates a new BackupItem instance.
	/// This is the only way to instantiate the class.
	/// </summary>
	public static BackupItem Create(ISourceContent content, string originalFileName, string? relativePath = null)
	{
		ArgumentNullException.ThrowIfNull(content);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(originalFileName);

		// Initialize essential metadata
		BackupMetadata metadata = new();
		metadata.Set(MetadataKey.SourceFileName, originalFileName);
		metadata.Set(MetadataKey.Length, content.Length);

		if(!string.IsNullOrWhiteSpace(relativePath))
		{
			metadata.Set(MetadataKey.SourceRelativePath, relativePath);
		}

		return new BackupItem(content, metadata);
	}
	public void Fail(string message, string stepName, Exception? ex = null)
	{
		State = BackupState.Failed;
		ProcessState = BackupProcessState.Analyzed; // Or another appropriate state
		Action = BackupActionType.Unknown; // Or Error, depending on your design
		ErrorInfo errorInfo = new();
		//ErrorInfo.AddError(stepName, message, DateTime.UtcNow, ex);
		// TODO: Implement adding error to ErrorInfo

	}

	/// <summary>
	/// Replaces the current content source.
	/// Used only when downloading from a remote device to a local temporary file.
	/// </summary>
	public void ReplaceContent(ISourceContent newContent)
	{
		Content = newContent ?? throw new ArgumentNullException(nameof(newContent));
	}
}