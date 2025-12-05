using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Errors;
using BMTP3.Core2.BackupNew.Job;

namespace BMTP3.Core2.BackupNew.Models;

/// <summary>
/// Concrete implementation of IBackupItem.
/// Encapsulates content, metadata, lifecycle and result state.
/// </summary>
public class BackupItem : IBackupItem
{
	public ISourceContent Content { get; private set; }
	public BackupMetadata Metadata { get; private set; }

	/// <summary>
	/// Lifecycle state of the item (systemic progression).
	/// </summary>
	public LifecycleState LifecycleState { get; private set; }

	/// <summary>
	/// Final outcome of the backup attempt for this item.
	/// </summary>
	public ResultState ResultState { get; private set; }

	/// <summary>
	/// Optional error information if the item failed.
	/// </summary>
	public ErrorInfo? ErrorInfo { get; private set; }

	// Private constructor – all object state is initialized here
	private BackupItem(ISourceContent content, BackupMetadata metadata)
	{
		ArgumentNullException.ThrowIfNull(content);
		ArgumentNullException.ThrowIfNull(metadata);

		Content = content;
		Metadata = metadata;

		LifecycleState = LifecycleState.New;
		ResultState = ResultState.Pending;
		ErrorInfo = null;
	}

	/// <summary>
	/// Creates a new BackupItem instance.
	/// </summary>
	public static BackupItem Create(ISourceContent content, string originalFileName, string? relativePath = null)
	{
		ArgumentNullException.ThrowIfNull(content);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(originalFileName);

		BackupMetadata metadata = new();
		metadata.Set(MetadataKey.SourceFileName, originalFileName);
		metadata.Set(MetadataKey.Length, content.Length);

		if(!string.IsNullOrWhiteSpace(relativePath))
		{
			metadata.Set(MetadataKey.SourceRelativePath, relativePath);
		}

		return new BackupItem(content, metadata);
	}

	/// <summary>
	/// Marks the item as failed with error info.
	/// </summary>
	public void Fail(string message, string stepName, Exception? ex = null)
	{
		ResultState = ResultState.Failed;
		LifecycleState = LifecycleState.Processed;

		ErrorInfo = new ErrorInfo();
		// ErrorInfo.AddError(stepName, message, DateTime.UtcNow, ex);
	}

	/// <summary>
	/// Replaces the current content source.
	/// </summary>
	public void ReplaceContent(ISourceContent newContent)
	{
		Content = newContent ?? throw new ArgumentNullException(nameof(newContent));
	}
}