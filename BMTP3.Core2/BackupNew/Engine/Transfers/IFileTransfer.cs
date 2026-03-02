using BMTP3.Core2.BackupNew.Engine.Models;

namespace BMTP3.Core2.BackupNew.Engine.Transfers;

// Transfer from staging to final target; should support DryRun and return an operation result.
public interface IFileTransfer
{
	Task<OperationResult> TransferAsync(string stagingPath, string targetPath, bool dryRun, CancellationToken ct);
}