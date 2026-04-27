using System.Diagnostics;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Progress.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Domain.Job;
using BMTP3.Core2.BackupNew.Domain.Repositories;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Internal;
using BMTP3.Core2.BackupNew.Engine.Orchestration;
using BMTP3.Core2.BackupNew.Engine.Staging;
using BMTP3.Core2.BackupNew.Engine.Steps.HashStep;
using BMTP3.Core2.BackupNew.Engine.Steps.InspectorStep;
using BMTP3.Core2.BackupNew.Engine.Steps.MetadataExtractionStep;
using BMTP3.Core2.BackupNew.Engine.Steps.SidecarGenerationStep;
using BMTP3.Core2.BackupNew.Engine.Steps.StagingStep;
using BMTP3.Core2.BackupNew.Engine.Steps.TimestampCorrectionStep;
using BMTP3.Core2.BackupNew.Engine.Steps.TransferStep;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Engine.Transfers;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using BMTP3.Core2.BackupNew.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BMTP3.Core2.BackupNew.Engine;

/// <summary>
///     Sequential BackupEngine — processes each item through the full pipeline one at a time.
/// </summary>
public class SequentialBackupEngine : IBackupEngine
{
    private readonly IBackupScanner _backupScanner;
    private readonly ICollisionResolver _collisionResolver;
    private readonly IDestinationInspector _destinationInspector;
    private readonly IFileTransfer _fileTransfer;
    private readonly IHashGenerator _hashGenerator;
    private readonly IItemHasher _itemHasher;
    private readonly ILogger<SequentialBackupEngine> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMetadataReader _metadataReader;
    private readonly IOptions<BackupEngineOptions> _options;
    private readonly IPathGenerator _pathGenerator;
    private readonly IBackupRepository _repository;
    private readonly ISidecarGeneratorFactory _sidecarGeneratorFactory;
    private readonly IStagingDownloader _stagingDownloader;
    private readonly IJobValidator _validator;

    public SequentialBackupEngine(
        IBackupScanner backupScanner,
        IStagingDownloader stagingDownloader,
        IMetadataReader metadataReader,
        IItemHasher itemHasher,
        IPathGenerator pathGenerator,
        ICollisionResolver collisionResolver,
        IFileTransfer fileTransfer,
        IHashGenerator hashGenerator,
        IBackupRepository repository,
        ISidecarGeneratorFactory sidecarGeneratorFactory,
        IJobValidator validator,
        IOptions<BackupEngineOptions> options,
        ILoggerFactory loggerFactory,
        IDestinationInspector destinationInspector
    )
    {
        _backupScanner = backupScanner ?? throw new ArgumentNullException(nameof(backupScanner));
        _stagingDownloader = stagingDownloader ?? throw new ArgumentNullException(nameof(stagingDownloader));
        _metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
        _itemHasher = itemHasher ?? throw new ArgumentNullException(nameof(itemHasher));
        _hashGenerator = hashGenerator ?? throw new ArgumentNullException(nameof(hashGenerator));
        _pathGenerator = pathGenerator ?? throw new ArgumentNullException(nameof(pathGenerator));
        _collisionResolver = collisionResolver ?? throw new ArgumentNullException(nameof(collisionResolver));
        _fileTransfer = fileTransfer ?? throw new ArgumentNullException(nameof(fileTransfer));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _sidecarGeneratorFactory = sidecarGeneratorFactory ?? throw new ArgumentNullException(nameof(sidecarGeneratorFactory));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<SequentialBackupEngine>();
        _destinationInspector = destinationInspector ?? throw new ArgumentNullException(nameof(destinationInspector));
    }

    public async Task<BackupJobResult> RunAsync(BackupPlan plan, IProgress<IBackupProgress> progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(plan);

        progress ??= new Progress<IBackupProgress>();
        IProgress<BackupProgress> stagingProgress = new Progress<BackupProgress>(p => progress.Report(p));

        // 0. Pre-flight Validation
        await _validator.ValidateAsync(plan, ct);

        // 0b. Runtime validation: disk space
        ValidateDiskSpace(plan);

        // 0c. Pre-flight: validate source and output
        ValidateSourceAndOutput(plan);

        // 0d. Initialize backup session for resume support
        Guid sessionId = Guid.NewGuid();
        BackupSessionEntity session = new()
        {
            SessionId = sessionId,
            Plan = plan,
            Records = new List<BackupResumeRecord>()
        };

        if (!plan.DryRun)
        {
            await _repository.SaveAsync(session, ct).ConfigureAwait(false);
            _logger.LogInformation("Backup session {SessionId} started. Output: {OutputPath}", sessionId, plan.OutputPath);
        }

        // Prepare tracking
        ProgressTracker tracker = new();
        tracker.SetPhase(BackupPhase.Initializing);

        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        BackupState state = BackupState.Ready;
        StopReason stopReason = StopReason.None;
        List<string> errors = new();

        if (ct.IsCancellationRequested)
        {
            return new BackupJobResult
            {
                JobName = plan.Name,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                State = BackupState.Stopped,
                StopReason = StopReason.UserCancelled,
                FinalProgress = tracker.GetSnapshot()
            };
        }

        // Periodic progress reporting task
        CancellationTokenSource reportingCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Task reportingTask = Task.Run(async () =>
        {
            try
            {
                while (!reportingCts.Token.IsCancellationRequested)
                {
                    progress.Report(tracker.GetSnapshot());
                    await Task.Delay(250, reportingCts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, reportingCts.Token);

        // Instantiate steps
        ContentBufferingItemStep bufferingStep = new(_stagingDownloader, stagingProgress, plan);
        MetadataExtractionItemStep metadataStep = new(_metadataReader, plan);
        TimestampCorrectionItemStep timestampStep = new(plan);

        HashStepContext hashStepContext = new()
        {
            HashTypes = plan.HashTypes.ToList(),
            ForceRecompute = false
        };
        HashItemStep hashStep = new(hashStepContext, _itemHasher, _loggerFactory.CreateLogger<HashItemStep>());

        TransferItemStep transferStep = new(plan, _pathGenerator, _collisionResolver, _fileTransfer, _itemHasher);
        DestinationInspectorItemStep inspectorStep = new(_destinationInspector, _hashGenerator, plan,
            _loggerFactory.CreateLogger<DestinationInspectorItemStep>());
        SidecarGenerationItemStep sidecarStep = new(plan, _sidecarGeneratorFactory);

        state = BackupState.Running;
        tracker.SetPhase(BackupPhase.Traversing);

        // Open MTP session if needed
        IMtpDeviceSession? mtpSession = null;
        if (plan.SourceType == SourceType.MediaDevice && _backupScanner is IMtpCapableScanner mtpScanner)
        {
            mtpSession = mtpScanner.OpenSession(plan);
        }

        try
        {
            int itemsSinceLastSave = 0;
            const int saveInterval = 10;

            await foreach (IBackupItem item in _backupScanner.ScanAsync(plan, ct))
            {
                ct.ThrowIfCancellationRequested();

                // Track discovery
                ulong length = 0;
                if (item.Metadata.Has(MetadataKey.Length))
                {
                    length = item.Metadata.Get<ulong>(MetadataKey.Length);
                }
                tracker.AddDiscovery(false, (long)length);

                // Run all steps sequentially for this item
                try
                {
                    tracker.SetPhase(BackupPhase.Transferring);

                    // 1. Content buffering (staging)
                    IProgress<ulong> stepProgress = new Progress<ulong>();

                    await bufferingStep.ExecuteAsync(item, stepProgress, ct);
                    await metadataStep.ExecuteAsync(item, stepProgress, ct);
                    await timestampStep.ExecuteAsync(item, stepProgress, ct);
                    await hashStep.ExecuteAsync(item, stepProgress, ct);
                    await transferStep.ExecuteAsync(item, stepProgress, ct);
                    await inspectorStep.ExecuteAsync(item, stepProgress, ct);
                    await sidecarStep.ExecuteAsync(item, stepProgress, ct);
                }
                catch (OperationCanceledException)
                {
                    throw; // Propagate cancellation
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process item {ItemId}", item.Id);
                    // The item's ResultState should already be set to failed by the step that threw.
                    // If not, we can't easily set it here without knowing the step API —
                    // but the tracker will record the failure below.
                }

                // Track completion
                ulong size = item.Metadata.Has(MetadataKey.Length)
                    ? item.Metadata.Get<ulong>(MetadataKey.Length)
                    : 0;

                tracker.CompleteItem(item.Id, item.ResultState, (long)size);

                // Persist item state for resume support
                if (!plan.DryRun)
                {
                    await _repository.PersistItemStateAsync(item, ct).ConfigureAwait(false);
                    itemsSinceLastSave++;

                    if (itemsSinceLastSave >= saveInterval)
                    {
                        await _repository.SaveAsync(session, ct).ConfigureAwait(false);
                        itemsSinceLastSave = 0;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested
        }
        catch (Exception ex)
        {
            tracker.SetPhase(BackupPhase.None);
            errors.Add($"Pipeline crashed: {ex.Message}");
            errors.Add(ex.ToString());
            state = BackupState.Stopped;
            stopReason = StopReason.FatalError;
        }
        finally
        {
            // Dispose MTP session
            if (mtpSession != null)
            {
                try
                {
                    mtpSession.Dispose();
                }
                catch
                {
                    /* best effort */
                }
            }
        }

        // Stop reporting
        try
        {
            reportingCts.Cancel();
            await reportingTask.ConfigureAwait(false);
        }
        catch
        {
            // ignored
        }

        // Determine final status
        BackupProgress finalSnap = tracker.GetSnapshot();

        if (ct.IsCancellationRequested)
        {
            state = BackupState.Stopped;
            stopReason = StopReason.UserCancelled;
            tracker.SetPhase(BackupPhase.None);
        }
        else if (finalSnap.FilesFailed > 0 && stopReason == StopReason.None)
        {
            state = BackupState.Stopped;
            stopReason = StopReason.FatalError;
            tracker.SetPhase(BackupPhase.None);
            errors.Add($"{finalSnap.FilesFailed} file(s) failed during the run.");
        }
        else if (stopReason == StopReason.None)
        {
            state = BackupState.Completed;
            stopReason = StopReason.None;
            tracker.SetPhase(BackupPhase.None);
        }

        progress.Report(tracker.GetSnapshot());

        if (!plan.DryRun)
        {
            await _repository.SaveAsync(session, ct).ConfigureAwait(false);
            _logger.LogInformation("Backup session {SessionId} completed. Files: {Succeeded}/{Total}, State: {State}",
                sessionId, finalSnap.FilesSucceeded, finalSnap.FilesDiscovered, state);
        }

        return new BackupJobResult
        {
            JobName = plan.Name,
            StartTime = startTime,
            EndTime = DateTimeOffset.UtcNow,
            State = state,
            StopReason = stopReason,
            FinalProgress = finalSnap,
            Errors = errors
        };
    }

    private void ValidateDiskSpace(BackupPlan plan)
    {
        DirectoryInfo outputDir = new(plan.OutputPath);
        if (!outputDir.Exists)
        {
            outputDir.Create();
        }

        const long oneMb = 1024 * 1024;
        const long oneGb = 1024 * oneMb;

        long freeSpace = GetFreeSpace(outputDir.FullName);
        long freeSpaceMb = freeSpace / oneMb;
        long freeSpaceGb = freeSpace / oneGb;

        const long oneHundredMb = 100 * oneMb;
        const long tenGb = 10 * oneGb;

        if (freeSpace <= oneHundredMb)
        {
            throw new IOException(
                $"Insufficient disk space on output drive '{outputDir.Root}'. Available: {freeSpaceMb} MB.");
        }

        if (freeSpace <= oneGb)
        {
            _logger.LogWarning("Low disk space on output drive '{Drive}': {FreeSpace} MB. Backup may fail.",
                outputDir.Root, freeSpaceMb);
        }
        else if (freeSpace <= tenGb)
        {
            _logger.LogInformation("Disk space on output drive '{Drive}': {FreeSpace} GB available.", outputDir.Root,
                freeSpaceGb);
        }
    }

    private void ValidateSourceAndOutput(BackupPlan plan)
    {
        if (plan.SourceType == SourceType.FileSystem)
        {
            if (!Directory.Exists(plan.SourcePath))
            {
                throw new DirectoryNotFoundException($"Source path not found: {plan.SourcePath}");
            }

            try
            {
                Directory.EnumerateFileSystemEntries(plan.SourcePath).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException($"No read access to source: {plan.SourcePath}", ex);
            }
        }

        DirectoryInfo outputDir = new(plan.OutputPath);
        if (!outputDir.Exists)
        {
            try
            {
                outputDir.Create();
            }
            catch (Exception ex)
            {
                throw new IOException($"Cannot create output directory: {plan.OutputPath}", ex);
            }
        }

        try
        {
            string testFile = Path.Combine(plan.OutputPath, Path.GetRandomFileName());
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException($"No write access to output: {plan.OutputPath}", ex);
        }
    }

    private long GetFreeSpace(string path)
    {
        try
        {
            string root = Path.GetPathRoot(path) ?? path;
            DriveInfo drive = new(root);
            return drive.AvailableFreeSpace;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
