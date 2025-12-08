using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Errors;

namespace BMTP3.Core2.BackupNew.Domain.Item;

/// <summary>
/// Concrete implementation of IBackupItem.
/// Encapsulates content, metadata, lifecycle and result state.
/// </summary>
public class BackupItem : IBackupItem
{
	public IContent Content { get; private set; }

	/// <summary>
	/// Flexible property bag enriched throughout the pipeline.
	/// </summary>
	public BackupMetadata Metadata { get; private set; }

	public ItemLifecycleState LifecycleState { get; private set; }
	public ItemResultState ResultState { get; private set; }
	public ErrorLog Errors { get; } = new ErrorLog();

	private readonly List<AuditItemEntry> _auditTrail;
	public IReadOnlyList<AuditItemEntry> AuditTrail => _auditTrail.AsReadOnly();

	public uint AttemptCount { get; private set; }

	private BackupItem(IContent content, BackupMetadata metadata)
	{
		ArgumentNullException.ThrowIfNull(content);
		ArgumentNullException.ThrowIfNull(metadata);

		Content = content;
		Metadata = metadata;

		LifecycleState = ItemLifecycleState.New;
		ResultState = ItemResultState.Pending;
		_auditTrail = new List<AuditItemEntry>();
		AttemptCount = 0;
	}

	public static BackupItem Create(IContent content, string originalFileName, string? relativePath = null)
	{
		ArgumentNullException.ThrowIfNull(content);
		ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);

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
		ResultState = ItemResultState.Failed;
		LifecycleState = ItemLifecycleState.Processed;
		AttemptCount++;

		Errors.AddError(stepName, message, DateTime.UtcNow, ex);

		_auditTrail.Add(new AuditItemEntry
		{
			Stage = stepName,
			AttemptCount = AttemptCount,
			ResultState = ResultState,
			LifecycleState = LifecycleState,
			Timestamp = DateTime.UtcNow,
			ErrorSummary = message
		});
	}

	public void ReplaceContent(IContent newContent)
	{
		Content = newContent ?? throw new ArgumentNullException(nameof(newContent));
	}

	// Internal helpers used by the state machine to keep mutations in one place:

	internal void SetQueued()
	{
		LifecycleState = ItemLifecycleState.Queued;
		ResultState = ItemResultState.Pending;

		_auditTrail.Add(new AuditItemEntry
		{
			Stage = "Queue",
			AttemptCount = AttemptCount,
			LifecycleState = LifecycleState,
			ResultState = ResultState,
			Timestamp = DateTime.UtcNow
		});
	}

	internal void SetActive()
	{
		LifecycleState = ItemLifecycleState.Active;
		AttemptCount++;

		_auditTrail.Add(new AuditItemEntry
		{
			Stage = "Activate",
			AttemptCount = AttemptCount,
			LifecycleState = LifecycleState,
			ResultState = ResultState,
			Timestamp = DateTime.UtcNow
		});
	}

	internal void SetResult(ItemResultState to, string? errorSummary = null)
	{
		ResultState = to;

		if(LifecycleState == ItemLifecycleState.Active)
			LifecycleState = ItemLifecycleState.Processed;

		_auditTrail.Add(new AuditItemEntry
		{
			Stage = "Result",
			AttemptCount = AttemptCount,
			LifecycleState = LifecycleState,
			ResultState = ResultState,
			Timestamp = DateTime.UtcNow,
			ErrorSummary = errorSummary
		});
	}
}
