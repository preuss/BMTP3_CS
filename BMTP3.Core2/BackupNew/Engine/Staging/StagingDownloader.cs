using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Progress.Enums;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Resilience;

namespace BMTP3.Core2.BackupNew.Engine.Staging;

/// <summary>
///     Downloads BackupItem content to a staging/temp file and replaces the item's IContent with a FileContent wrapper.
/// </summary>
public class StagingDownloader : IStagingDownloader
{
	private readonly IRetryPolicy _retryPolicy;

	public StagingDownloader(IRetryPolicy retryPolicy)
	{
		_retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
	}

	public async Task DownloadToStagingAsync(IBackupItem item, string stagingRoot, IProgress<BackupProgress>? progress,
		CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(item);
		if (string.IsNullOrWhiteSpace(stagingRoot))
		{
			throw new ArgumentException("Staging root cannot be null or empty.", nameof(stagingRoot));
		}

		// 1. Generate unique staging path preserving original extension
		string originalFileName = item.Metadata.Get<string>(MetadataKey.SourceFileName) ?? "unknown";
		string extension = Path.GetExtension(originalFileName);
		string stagingFileName = Guid.NewGuid().ToString("n") + extension;
		string stagingPath = Path.Combine(stagingRoot, stagingFileName);

		Directory.CreateDirectory(stagingRoot);

		// 2. Stream content from source to staging file with Retry Logic
		await _retryPolicy.ExecuteAsync(async () =>
		{
			using (Stream sourceStream = item.Content.OpenRead())
			using (FileStream destStream = File.Create(stagingPath))
			{
				await sourceStream.CopyToAsync(destStream, ct);
			}

			return true;
		}, ct);

		// 3. Replace item.Content with FileContent pointing to staged file
		FileContent stagedContent = new(new FileInfo(stagingPath));
		item.ReplaceContent(stagedContent);

		// 4. Update metadata to reflect staging
		item.Metadata.Set(MetadataKey.LocalTempPath, stagingPath);

		progress?.Report(
			new BackupProgress { Phase = BackupPhase.Transferring, ActiveFiles = new List<FileProgress>() });
	}
}