using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Engine.Models;

namespace BMTP3.Core2.BackupNew.Engine.Transfers;

// Simple local file transfer implementation: copies file from staging to target and deletes source.
public class LocalFileTransfer : IFileTransfer
{
	public Task<OperationResult> TransferAsync(string stagingPath, string targetPath, bool dryRun, CancellationToken ct)
	{
		if(string.IsNullOrWhiteSpace(stagingPath)) return Task.FromResult(OperationResult.Fail("stagingPath is empty"));
		if(string.IsNullOrWhiteSpace(targetPath)) return Task.FromResult(OperationResult.Fail("targetPath is empty"));
		if(dryRun) return Task.FromResult(OperationResult.Ok());

		try
		{
			var destDir = Path.GetDirectoryName(targetPath);
			if(!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);

			// Copy and overwrite if exists, then delete source.
			// TODO: Use Rename for exception if not atomic, and then Move, then we know this will not be atomic but better than copy+delete, worst of the worst.
			File.Copy(stagingPath, targetPath, true);
			File.Delete(stagingPath);
			return Task.FromResult(OperationResult.Ok());
		} catch(Exception ex)
		{
			return Task.FromResult(OperationResult.Fail(ex.Message));
		}
	}
}