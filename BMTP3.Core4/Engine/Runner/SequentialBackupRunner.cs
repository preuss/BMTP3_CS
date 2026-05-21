using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Session;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;

namespace BMTP3.Core4.Engine.Runner;

internal sealed class SequentialBackupRunner : IBackupRunner
{
	public async Task<BackupResult> RunAsync(
		BackupRunnerRequest request,
		BackupSessionKey sessionKey,
		IProgress<BackupRunnerProgress>? progress,
		CancellationToken cancellationToken
	)
	{
		ArgumentNullException.ThrowIfNull(request);

		IReadOnlyList<BackupRecord> records = request.Records;
		int total = records.Count;
		int succeeded = 0;
		int skipped = 0;
		int failed = 0;

		BackupRunnerProgress currentProgress = new()
		{
			CurrentPhase = BackupProgressPhase.Transferring,
			TotalFilesSelected = total,
		};
		progress?.Report(currentProgress);

		List<BackupResultItem> resultItems = new(capacity: total);

		foreach(BackupRecord record in records)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// Guard against invalid states that may indicate a crash during a previous run. Only Pending records should be present at this point.
			switch(record.Status)
			{
				case BackupItemStatus.Pending:
					// Only Pending records are processed — fall through to processing below.
					break;

				case BackupItemStatus.Active:
					throw new InvalidOperationException($"Record '{record.Item.Id}' has status Active at run start. This may indicate a crash during a previous run.");

				case BackupItemStatus.Succeeded:
				case BackupItemStatus.Failed:
				case BackupItemStatus.Skipped:
					continue;

				default:
					throw new InvalidOperationException($"Unexpected BackupItemStatus '{record.Status}' for record '{record.Item.Id}'.");
			}

			BackupItem item = record.Item;

			currentProgress = currentProgress with
			{
				ActiveFiles = new[]
				{
					new BackupProgressItem
					{
						RelativePath = item.RelativePath,
						Length = (long)item.Content.Length,
						Phase = BackupProgressItemPhase.Transferring,
					}
				}
			};
			progress?.Report(currentProgress);

			// TODO: Actual file transfer + sidecar generation
			record.Status = BackupItemStatus.Succeeded;
			record.StatusChangedAt = DateTimeOffset.UtcNow;

			succeeded++;

			resultItems.Add(new BackupResultItem
			{
				Id = item.Id,
				SourcePath = item.SourcePath,
				DestinationPath = record.DestinationPath,
				Length = (long)item.Content.Length,
				State = BackupResultItemState.Succeeded,
			});

			currentProgress = currentProgress with
			{
				FilesSucceeded = succeeded,
				ActiveFiles = Array.Empty<BackupProgressItem>(),
			};
			progress?.Report(currentProgress);
		}

		return new BackupResult
		{
			State = BackupResultState.Completed,
			ItemResults = resultItems,
		};
	}
}
