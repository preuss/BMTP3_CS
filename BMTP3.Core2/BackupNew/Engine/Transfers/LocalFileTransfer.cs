using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Engine.Models;

using BMTP3.Core2.BackupNew.Engine.Resilience;

namespace BMTP3.Core2.BackupNew.Engine.Transfers;

// Simple local file transfer implementation: copies file from staging to target and deletes source.
public class LocalFileTransfer : IFileTransfer
{
    private readonly IRetryPolicy _retryPolicy;

    public LocalFileTransfer(IRetryPolicy retryPolicy)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
    }

	public async Task<OperationResult> TransferAsync(string stagingPath, string targetPath, bool dryRun, CancellationToken ct)
	{
		if(string.IsNullOrWhiteSpace(stagingPath)) return OperationResult.Fail("stagingPath is empty");
		if(string.IsNullOrWhiteSpace(targetPath)) return OperationResult.Fail("targetPath is empty");
		if(dryRun) return OperationResult.Ok();

		try
		{
			var destDir = Path.GetDirectoryName(targetPath);
			if(!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);

            // 1. Copy using Stream for Async/Cancellation support and Retry logic
            await _retryPolicy.ExecuteAsync(async () => 
            {
                using (var sourceStream = new FileStream(stagingPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                using (var destStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await sourceStream.CopyToAsync(destStream, ct);
                }
                return true;
            }, ct);

			// 2. Delete source after successful copy
			File.Delete(stagingPath);
			return OperationResult.Ok();
		} catch(Exception ex)
		{
			return OperationResult.Fail(ex.Message);
		}
	}
}