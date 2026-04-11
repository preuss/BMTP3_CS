using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Staging;

namespace BMTP3.Core2.BackupNew.Engine.Steps.StagingStep;

/// <summary>
///     Represents a step that buffers content from the source to a local temporary file.
///     This ensures content is localized before further processing.
/// </summary>
public class ContentBufferingItemStep : IBackupItemStep<BackupPlan, bool>
{
	private readonly IStagingDownloader _downloader;
	private readonly IProgress<BackupProgress>? _progress;
	private readonly string _stagingRoot;

	public ContentBufferingItemStep(IStagingDownloader downloader, IProgress<BackupProgress>? progress,
		BackupPlan context)
	{
		_downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
		_progress = progress; // Can be null, will be handled by the downloader
		Context = context ?? throw new ArgumentNullException(nameof(context));
		// Generate a unique staging root for each instance of the step
		_stagingRoot = Path.Combine(Path.GetTempPath(), "bmtp3_staging", Guid.NewGuid().ToString("n"));
	}

	public string Name => "Content Buffering";
	public FilePhase Phase => FilePhase.Staging;

	public BackupPlan Context { get; }

	/// <summary>
	///     Executes the content buffering operation. Downloads the item's content to a local temp file.
	/// </summary>
	/// <param name="item">The backup item to buffer.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>True if buffering was successful, otherwise false (or throws exception).</returns>
	public async Task<bool> ExecuteAsync(IBackupItem item, IProgress<ulong> progress, CancellationToken ct)
	{
        // In DryRun we must avoid any filesystem side-effects including creating staging files.
		if (Context.DryRun)
		{
			item.AddLog("Staging skipped (DryRun).", Name);
			return true;
		}

		await _downloader.DownloadToStagingAsync(item, _stagingRoot, _progress, ct);
		return true;
	}
}