using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Exceptions;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;
using BMTP3.Core4.State;

namespace BMTP3.Core4.Engine.Session;

/// <summary>
/// Default implementation of <see cref="ISessionStateService"/>.
/// Uses an <see cref="ISummaryStore"/> for persistence and
/// owns the <see cref="BackupRecord"/> ↔ <see cref="BackupSummaryItem"/> mapping.
/// </summary>
internal sealed class SessionStateService : ISessionStateService
{
	private readonly ISummaryStore _store;

	public SessionStateService(ISummaryStore store)
	{
		_store = store;
	}

	/// <inheritdoc />
	public async Task ApplyResumeAsync(
		IReadOnlyList<BackupRecord> records,
		BackupSessionKey sessionKey,
		SessionResumeStrategy resumeBehavior,
		CancellationToken cancellationToken
	)
	{
		BackupSummary? summary = await _store.LoadAsync();
		if(summary is null) return;

		Dictionary<string, BackupSummaryItem> summaryById = summary.Items.ToDictionary(i => i.Id);
		HashSet<string> summaryIds = new(summaryById.Keys);
		HashSet<string> recordIds = records.Select(r => r.Item.Id).ToHashSet();

		int added = recordIds.Except(summaryIds).Count();
		int removed = summaryIds.Except(recordIds).Count();

		if(added > 0 || removed > 0)
		{
			switch(resumeBehavior)
			{
				case SessionResumeStrategy.Abort:
					throw new SessionResumeMismatchException(added, removed);

				case SessionResumeStrategy.Restart:
					await _store.DeleteAsync();
					return;

				case SessionResumeStrategy.Continue:
					break;

				default:
					throw new InvalidOperationException($"Unsupported resume behavior: {resumeBehavior}");
			}
		}

		foreach(BackupRecord record in records)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if(summaryById.TryGetValue(record.Item.Id, out BackupSummaryItem? match))
			{
				record.DestinationPath = match.DestinationPath;
				record.Status = ToBackupItemStatus(match.Status);
				record.StatusChangedAt = match.CompletedAt;
			}
		}

		// Post-resume guard: Active/Failed must not survive resume
		foreach(BackupRecord record in records)
		{
			cancellationToken.ThrowIfCancellationRequested();
			switch(record.Status)
			{
				case BackupItemStatus.Pending:
				case BackupItemStatus.Succeeded:
				case BackupItemStatus.Skipped:
					//Legal status.
					break;

				case BackupItemStatus.Active:
					throw new InvalidOperationException($"Record '{record.Item.Id}' has status Active after resume. This may indicate a crash during a previous run.");

				case BackupItemStatus.Failed:
					throw new InvalidOperationException($"Record '{record.Item.Id}' has status Failed after resume. Failed items should have been converted to Pending during resume.");

				default:
					// Unknown status
					throw new InvalidOperationException($"Unexpected BackupItemStatus '{record.Status}' for record '{record.Item.Id}'.");
			}
		}

		// Persist updated state so summary matches records after resume
		await SaveAsync(records, sessionKey, cancellationToken);
	}

	/// <inheritdoc />
	public async Task SaveAsync(
		IReadOnlyList<BackupRecord> records,
		BackupSessionKey sessionKey,
		CancellationToken cancellationToken
	)
	{
		BackupSummary summary = new()
		{
			SessionId = sessionKey.SessionId,
			SourceRoot = sessionKey.SourceIdentity,
			CreatedAt = DateTimeOffset.UtcNow,
			Items = records.Select(ToSummaryItem).ToList(),
		};

		await _store.SaveAsync(summary, cancellationToken);
	}

	/// <inheritdoc />
	public Task DeleteAsync()
	{
		return _store.DeleteAsync();
	}

	/// <summary>
	/// Converts a <see cref="BackupRecord"/> to a <see cref="BackupSummaryItem"/> for persistence.
	/// </summary>
	private static BackupSummaryItem ToSummaryItem(BackupRecord record)
	{
		long length = record.Item.Content.Length > (ulong)long.MaxValue
			? long.MaxValue
			: (long)record.Item.Content.Length;

		return new BackupSummaryItem
		{
			Id = record.Item.Id,
			SourcePath = record.Item.SourcePath,
			RelativeFilePath = record.Item.RelativeFilePath,
			FileName = record.Item.FileName,
			Length = length,
			LastModified = record.Item.DateModified,
			DateCreated = record.Item.DateCreated,
			DateAuthored = record.Item.DateAuthored,
			DestinationPath = record.DestinationPath,
			Status = ToSummaryItemStatus(record.Status),
			IsCompleted = record.Status is BackupItemStatus.Succeeded or BackupItemStatus.Skipped,
			CompletedAt = record.StatusChangedAt,
		};
	}

	/// <summary>
	/// Maps internal <see cref="BackupItemStatus"/> to persisted <see cref="BackupSummaryItemStatus"/>.
	/// Active and Failed are mapped to Pending so they will be retried on resume.
	/// </summary>
	private static BackupSummaryItemStatus ToSummaryItemStatus(BackupItemStatus status)
	{
		switch(status)
		{
			case BackupItemStatus.Succeeded:
				return BackupSummaryItemStatus.Succeeded;
			case BackupItemStatus.Skipped:
				return BackupSummaryItemStatus.Skipped;
			case BackupItemStatus.Failed:
			case BackupItemStatus.Pending:
			case BackupItemStatus.Active:
				return BackupSummaryItemStatus.Pending;
			default:
				throw new InvalidOperationException($"Unexpected BackupItemStatus '{status}'.");
		}
	}

	/// <summary>
	/// Maps persisted <see cref="BackupSummaryItemStatus"/> back to internal <see cref="BackupItemStatus"/>.
	/// </summary>
	private static BackupItemStatus ToBackupItemStatus(BackupSummaryItemStatus status)
	{
		return status switch
		{
			BackupSummaryItemStatus.Succeeded => BackupItemStatus.Succeeded,
			BackupSummaryItemStatus.Skipped => BackupItemStatus.Skipped,
			BackupSummaryItemStatus.Pending => BackupItemStatus.Pending,
			_ => throw new InvalidOperationException($"Unexpected BackupSummaryItemStatus '{status}'."),
		};
	}
}
