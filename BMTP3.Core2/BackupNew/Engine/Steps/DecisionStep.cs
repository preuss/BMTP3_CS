using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Infrastructure.Repositories;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;

namespace BMTP3.Core2.BackupNew.Engine.Steps;

public class DecisionStep : IBackupStep
{
    private readonly IPathGenerator _pathGenerator;
    private readonly ICollisionResolver _collisionResolver;
    private readonly IEnumerable<BackupResumeRecord> _history;

    public DecisionStep(IPathGenerator pathGenerator, ICollisionResolver collisionResolver, IEnumerable<BackupResumeRecord> history)
    {
        _pathGenerator = pathGenerator;
        _collisionResolver = collisionResolver;
        _history = history ?? Enumerable.Empty<BackupResumeRecord>();
    }

    public string Name => "Decision";

    public async Task ExecuteAsync(IBackupItem item, BackupPlan plan, CancellationToken ct)
    {
        try
        {
            string sourcePath = item.Metadata.Get<string>(MetadataKey.SourceFullPath) ?? "";
            
            var historyRecord = _history.FirstOrDefault(r => r.SourcePath == sourcePath && r.State == PersistState.Completed);

            if (historyRecord != null)
            {
                item.SetResult(ItemResultState.Skipped, "Already completed in previous session run.");
                return;
            }

            string relativePath = _pathGenerator.GenerateRelativePath(item, plan);
            string fullTargetPath = Path.Combine(plan.OutputPath, relativePath);
            
            item.Metadata.Set(MetadataKey.FinalTargetPath, fullTargetPath);

            if (plan.DryRun)
            {
                item.Metadata.Set(MetadataKey.BackupAction, BackupActionType.Copy);
                return;
            }

            CollisionResult resolution = await _collisionResolver.ResolveAsync(item, fullTargetPath, plan, ct);

            item.Metadata.Set(MetadataKey.BackupAction, resolution.Action);
            item.Metadata.Set(MetadataKey.FinalTargetPath, resolution.TargetPath); 

            if (resolution.Action == BackupActionType.Skip)
            {
                item.SetResult(ItemResultState.Skipped, resolution.Reason);
            }
        }
        catch (Exception ex)
        {
            item.Fail($"Decision failed: {ex.Message}", Name, ex);
        }
    }
}