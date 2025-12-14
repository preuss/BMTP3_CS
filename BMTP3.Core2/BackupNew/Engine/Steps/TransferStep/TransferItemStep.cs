using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Engine.Transfers;
using BMTP3.Core2.BackupNew.Engine.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps.TransferStep;

public class TransferItemStep : IBackupItemStep<BackupPlan, OperationResult>
{
    private readonly IPathGenerator _pathGenerator;
    private readonly ICollisionResolver _collisionResolver;
    private readonly IFileTransfer _fileTransfer;

    public string Name => "Transfer";

	private readonly BackupPlan _context;
	public BackupPlan Context { get; }
	public TransferItemStep(
		BackupPlan context,
		IPathGenerator pathGenerator, 
        ICollisionResolver collisionResolver, 
        IFileTransfer fileTransfer)
    {
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_pathGenerator = pathGenerator ?? throw new ArgumentNullException(nameof(pathGenerator));
        _collisionResolver = collisionResolver ?? throw new ArgumentNullException(nameof(collisionResolver));
        _fileTransfer = fileTransfer ?? throw new ArgumentNullException(nameof(fileTransfer));
    }

    public async Task<OperationResult> ExecuteAsync(IBackupItem item, CancellationToken ct)
    {
        // 1. Determine relative path
        string relativePath = _pathGenerator.GenerateRelativePath(item, _context);
        string destinationPath = Path.Combine(_context.OutputPath, relativePath);

        // 2. Resolve collisions
        CollisionResult collision = await _collisionResolver.ResolveAsync(item, destinationPath, _context, ct);

        // 3. Act based on collision result
        if (collision.Action == BackupActionType.Skip)
        {
            item.SetResult(ItemResultState.Skipped, collision.Reason);
            item.AddLog($"Skipped: {collision.Reason}", Name);
            return OperationResult.Skipped(collision.Reason);
        }

        if (collision.Action == BackupActionType.Rename)
        {
            destinationPath = collision.TargetPath; // Use the new unique path
            item.AddLog($"Renamed to: {Path.GetFileName(destinationPath)}", Name);
        }

        // 4. Transfer
        // Ensure we have a local file to transfer
        if (item.Content is not BMTP3.Core2.BackupNew.Content.FileContent fileContent)
        {
             string msg = $"Content is not a local file (found {item.Content?.GetType().Name}). Staging step might have failed.";
             item.Fail(msg, Name);
             return OperationResult.Fail(msg);
        }

        // Ensure directory exists
        string? destDir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir) && !_context.DryRun)
        {
            Directory.CreateDirectory(destDir);
        }

        OperationResult result = await _fileTransfer.TransferAsync(fileContent.FileInfo.FullName, destinationPath, _context.DryRun, ct);

        if (result.Success)
        {
            item.SetResult(ItemResultState.Success);
            item.Metadata.Set(MetadataKey.FinalTargetPath, destinationPath);
        }
        else
        {
            item.Fail("Transfer failed: " + result.Message, Name);
        }

        return result;
    }
}
