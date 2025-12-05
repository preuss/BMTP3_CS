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

	/// <summary>
	/// Flexible property bag enriched throughout the pipeline.
	/// </summary>
	public BackupMetadata Metadata { get; private set; }

	public LifecycleState LifecycleState { get; private set; }
	public ResultState ResultState { get; private set; }
	public ErrorInfo? ErrorInfo { get; private set; }

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

	public void Fail(string message, string stepName, Exception? ex = null)
	{
		ResultState = ResultState.Failed;
		LifecycleState = LifecycleState.Processed;

		ErrorInfo = new ErrorInfo();
		// ErrorInfo.AddError(stepName, message, DateTime.UtcNow, ex);
	}

	public void ReplaceContent(ISourceContent newContent)
	{
		Content = newContent ?? throw new ArgumentNullException(nameof(newContent));
	}
}
