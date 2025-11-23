using BMTP3.Core.BackupNew.Content;
using BMTP3.Core.BackupNew.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.BackupNew.Models;

/// <summary>
/// Concrete implementation of IBackupItem.
/// Fully encapsulated, immutable from the outside except through explicit methods.
/// </summary>
public class BackupItem : IBackupItem {
	public ISourceContent Content { get; private set; }
	public BackupMetadata Metadata { get; }
	public BackupState State { get; private set; }
	public ErrorInfo ErrorInfo { get; }

	// Private constructor – all object state is initialized here
	private BackupItem(ISourceContent content, BackupMetadata metadata) {
		ArgumentNullException.ThrowIfNull(content);
		ArgumentNullException.ThrowIfNull(metadata);
		Content = content;
		Metadata = metadata;
		State = BackupState.Pending;
		ErrorInfo = new ErrorInfo();
	}

	/// <summary>
	/// Creates a new BackupItem instance.
	/// This is the only way to instantiate the class.
	/// </summary>
	public static BackupItem Create(ISourceContent content, string originalFileName, string? relativePath = null) {
		ArgumentNullException.ThrowIfNull(content);
		if(string.IsNullOrWhiteSpace(originalFileName)) {
			throw new ArgumentException("Original file name is required.", nameof(originalFileName));
		}

		var metadata = new BackupMetadata();
		metadata.Set(MetadataKey.OriginalFileName, originalFileName);
		metadata.Set(MetadataKey.Size, content.Length);

		if(!string.IsNullOrWhiteSpace(relativePath)) {
			metadata.Set(MetadataKey.SourceRelativePath, relativePath);
		}

		return new BackupItem(content, metadata);
	}

	/// <summary>
	/// Replaces the current content source.
	/// Used only when downloading from a remote device to a local temporary file.
	/// </summary>
	public void ReplaceContent(ISourceContent newContent) {
		Content = newContent ?? throw new ArgumentNullException(nameof(newContent));
	}

	/// <summary>
	/// Advances the item to a new processing state.
	/// Only forward progression is allowed, except for terminal states (Failed, Skipped).
	/// Thread-safe to prevent race conditions.
	/// </summary>
	public void AdvanceTo(BackupState newState)
	{
		lock(this)
		{
			if(newState < State && newState is not (BackupState.Failed or BackupState.Skipped))
			{
				throw new InvalidOperationException($"Cannot revert state from {State} to {newState}");
			}

			State = newState;
		}
	}
}