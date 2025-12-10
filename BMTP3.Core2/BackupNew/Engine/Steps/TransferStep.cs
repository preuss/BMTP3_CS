using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Engine.Steps;

public class TransferStep : IBackupStep
{
    public string Name => "Transfer";

    public async Task ExecuteAsync(IBackupItem item, BackupPlan plan, CancellationToken ct)
    {
        if (item.ResultState != ItemResultState.Pending) return;

        try
        {
            var action = item.Metadata.Get<BackupActionType>(MetadataKey.BackupAction);
            var targetPath = item.Metadata.Get<string>(MetadataKey.FinalTargetPath);

            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("Target path not set. Decision step might have failed.");
            }

            if (plan.DryRun)
            {
                item.SetResult(ItemResultState.Success, "Dry Run - Simulation");
                return;
            }

            if (action == BackupActionType.Copy || action == BackupActionType.Rename)
            {
                await PerformTransferAsync(item, targetPath, ct);
                item.SetResult(ItemResultState.Success);
            }
            else
            {
                item.SetResult(ItemResultState.Skipped, $"Action was {action}");
            }
        }
        catch (Exception ex)
        {
            item.Fail($"Transfer failed: {ex.Message}", Name, ex);
        }
    }

    private async Task PerformTransferAsync(IBackupItem item, string destinationPath, CancellationToken ct)
    {
        string? destDir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        if (item.Content is IMoveableContent moveable)
        {
            var newContent = moveable.MoveTo(destinationPath);
            item.ReplaceContent(newContent);
        }
        else
        {
            using var sourceStream = item.Content.OpenRead();
            using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            
            await sourceStream.CopyToAsync(destStream, ct);
            
            item.ReplaceContent(new FileContent(destinationPath));
        }

        if (item.Metadata.Has(MetadataKey.AuthoredDateTime))
        {
            try
            {
                var date = item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime);
                File.SetCreationTime(destinationPath, date);
                File.SetLastWriteTime(destinationPath, date);
            }
            catch 
            {
            }
        }
    }
}