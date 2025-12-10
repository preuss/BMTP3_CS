using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Domain.Job;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Engine.Steps;
using BMTP3.Core2.BackupNew.Infrastructure.Repositories;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.Api.Request;

namespace BMTP3.Core2.BackupNew.Engine;

/// <summary>
/// The main engine that orchestrates the backup process.
/// </summary>
public class BackupEngine : IBackupEngine
{
    private readonly IBackupScanner _scanner;
    private readonly IBackupRepository _repository;

    // Strategies are injected to be passed to steps
    private readonly IMetadataExtractor _metadataExtractor;
    private readonly IPathGenerator _pathGenerator;
    private readonly ICollisionResolver _collisionResolver;

    // Pipeline steps are injected to adhere to DIP and improve testability
    private readonly IEnumerable<IBackupStep> _steps;

    public BackupEngine(
        IBackupScanner scanner, 
        IBackupRepository repository,
        IMetadataExtractor metadataExtractor,
        IPathGenerator pathGenerator,
        ICollisionResolver collisionResolver,
        IEnumerable<IBackupStep> steps)
    {
        _scanner = scanner;
        _repository = repository;
        _metadataExtractor = metadataExtractor;
        _pathGenerator = pathGenerator;
        _collisionResolver = collisionResolver;
        _steps = steps ?? Array.Empty<IBackupStep>();
    }

    public async Task<BackupJobResult> RunAsync(BackupPlan plan, IProgress<BackupProgress> progress, CancellationToken ct)
    {
        // 1. Initialization
        var job = new BackupJob(plan.Name);
        job.TransitionTo(JobState.Running, "Starting backup");
        
        var stats = new BackupJobResult 
        { 
            JobName = plan.Name, 
            StartTime = DateTime.UtcNow,
            Status = JobState.Running
        };
        
        var report = new BackupProgress();

        // Load Session/History
        // In a real app, we might look up a specific session ID based on plan Name or create new.
        // For simplicity here, we assume the repository manages the "current" session context.
        var session = await _repository.LoadAsync();
        var historyRecords = session?.Records ?? new List<BackupResumeRecord>();

        // 2. Build Pipeline (steps are injected to allow DI and testability)
        var steps = _steps;

        try
        {
            // 3. Execution Loop
            int processedCount = 0;

            await foreach (var item in _scanner.ScanAsync(plan, ct))
            {
                if (ct.IsCancellationRequested) break;

                job.Items.Add((BackupItem)item); // Add to job tracking
                stats.TotalFilesScanned++;

                // Report Progress (Discovery)
                report.TotalItemsDiscovered++;
                report.CurrentActivity = $"Scanning: {item.Metadata.Get<string>(MetadataKey.SourceFileName)}";
                progress.Report(report);

                // --- Execute Steps ---
                foreach (var step in steps)
                {
                    // If item failed in previous step, skip remaining steps
                    if (item.ResultState == ItemResultState.Failed) break;
                    if (item.ResultState == ItemResultState.Skipped) break; // Optimization: Skip transfer if skipped

                    report.CurrentActivity = $"{step.Name}: {item.Metadata.Get<string>(MetadataKey.SourceFileName)}";
                    progress.Report(report);

                    await step.ExecuteAsync(item, plan, ct);
                }

                // --- Post-Process Item ---
                processedCount++;
                report.ItemsProcessed = processedCount;
                
                if (item.ResultState == ItemResultState.Success)
                {
                    stats.FilesCopied++;
                    stats.TotalBytesCopied += (long)item.Metadata.Get<ulong>(MetadataKey.Length);
                }
                else if (item.ResultState == ItemResultState.Skipped)
                {
                    stats.FilesSkipped++;
                }
                else if (item.ResultState == ItemResultState.Failed)
                {
                    stats.FilesFailed++;
                    // Add to stats errors
                    foreach(var err in item.Errors.Errors)
                    {
                        stats.FileErrors.Add(err.ToString());
                    }
                }

                // Update Progress
                progress.Report(report);

                // Artificial Delay (Throttling)
                if (plan.DelayMs > 0) await Task.Delay(plan.DelayMs, ct);
            }

            job.TransitionTo(JobState.Completed, "Backup completed");
            stats.Status = JobState.Completed;
        }
        catch (OperationCanceledException)
        {
            job.TransitionTo(JobState.Cancelled, "Backup cancelled by user");
            stats.Status = JobState.Cancelled;
        }
        catch (Exception ex)
        {
            job.TransitionTo(JobState.Failed, $"Critical failure: {ex.Message}");
            stats.Status = JobState.Failed;
            stats.GlobalErrors.Add(ex.Message);
            stats.GlobalError = ex.Message;
        }
        finally
        {
            stats.EndTime = DateTime.UtcNow;
            // Save Session (Persist the results for next time)
            // Here we would convert job.Items back to BackupResumeRecords and save.
            // await _repository.SaveAsync(...);
        }

        return stats;
    }
}