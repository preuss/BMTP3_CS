using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Staging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps.StagingStep;

/// <summary>
/// Represents a step that buffers content from the source to a local temporary file.
/// This ensures content is localized before further processing.
/// </summary>
public class ContentBufferingItemStep : IBackupItemStep<BackupPlan, bool>
{
    private readonly IStagingDownloader _downloader;
    private readonly IProgress<BackupProgress> _progress;
    private readonly string _stagingRoot;

    public string Name => "Content Buffering";

    public BackupPlan Context { get; private set; }

    public ContentBufferingItemStep(IStagingDownloader downloader, IProgress<BackupProgress> progress)
    {
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _progress = progress; // Can be null, will be handled by the downloader
        // Generate a unique staging root for each instance of the step
        _stagingRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bmtp3_staging", Guid.NewGuid().ToString("n"));
    }

    /// <summary>
    /// Executes the content buffering operation. Downloads the item's content to a local temp file.
    /// </summary>
    /// <param name="item">The backup item to buffer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if buffering was successful, otherwise false (or throws exception).</returns>
    public async Task<bool> ExecuteAsync(IBackupItem item, CancellationToken ct)
    {
        if (Context == null)
        {
            throw new InvalidOperationException("Context must be set before executing the step.");
        }

        await _downloader.DownloadToStagingAsync(item, _stagingRoot, _progress, ct);
        return true;
    }
}
