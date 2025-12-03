using BMTP3.Core2.BackupNew2.Interfaces;
using BMTP3.Core2.BackupNew2.Models.Configuration;

namespace BMTP3.Core2.BackupNew2.Models.Internal;

public class BackupItem : IBackupItem
{
	public ISourceContent Content { get; private set; }
	public Metadata Metadata { get; }
	public BackupState State { get; set; }
	public BackupProcessState ProcessState { get; set; }
	/// <summary>
	/// The final outcome of the item's processing (e.g., Completed, Skipped, Failed).
	/// This is set once the item reaches a terminal state.
	/// </summary>
	public BackupTerminalState TerminalState { get; set; }
	public BackupActionType Action { get; set; } = BackupActionType.Unknown;
	public ErrorInfo? ErrorInfo { get; set; }

	public BackupMetadata BackupMetadata => throw new NotImplementedException();

	// Private constructor to enforce usage of Create factory
	private BackupItem(ISourceContent content)
	{
		Content = content ?? throw new ArgumentNullException(nameof(content));
		Metadata = new Metadata();
		State = BackupState.New;
		TerminalState = BackupTerminalState.Unknown; // Initialize terminal state
	}
	public static IBackupItem Create(ISourceContent content, string originalFileName, string? relativePath = null)
	{
		var item = new BackupItem(content);

		// Initialize essential metadata
		item.Metadata.Set(MetadataKey.OriginalFileName, originalFileName);
		item.Metadata.Set(MetadataKey.Length, content.Length);

		if(!string.IsNullOrWhiteSpace(relativePath))
		{
			item.Metadata.Set(MetadataKey.SourceRelativePath, relativePath);
		}

		return item;
	}
	public void Fail(string message, string stepName, Exception? ex = null)
	{
		State = BackupState.Failed;
		ProcessState = BackupProcessState.Analyzed; // Or another appropriate state
		Action = BackupActionType.Unknown; // Or Error, depending on your design
		ErrorInfo = new ErrorInfo();
		ErrorInfo.AddError(stepName, message, DateTime.UtcNow, ex);

	}
}
