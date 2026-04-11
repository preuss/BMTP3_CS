using BMTP3.Core2.BackupNew.Engine.Models;
using BMTP3.Core2.BackupNew.Engine.Resilience;

namespace BMTP3.Core2.BackupNew.Engine.Transfers;

// Simple local file transfer implementation: copies file from staging to target.
// NOTE: Staging file deletion is NOT performed here - caller must handle cleanup
// after verification to ensure data integrity on verification failure.
public class LocalFileTransfer : IFileTransfer
{
	private readonly IRetryPolicy _retryPolicy;

	public LocalFileTransfer(IRetryPolicy retryPolicy)
	{
		_retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
	}

	public async Task<OperationResult> TransferAsync(string stagingPath, string targetPath, bool dryRun,
		CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(stagingPath))
		{
			return OperationResult.Fail("stagingPath is empty");
		}

		if (string.IsNullOrWhiteSpace(targetPath))
		{
			return OperationResult.Fail("targetPath is empty");
		}

		if (dryRun)
		{
			return OperationResult.Ok();
		}

		try
		{
			string? destDir = Path.GetDirectoryName(targetPath);
			if (!string.IsNullOrEmpty(destDir))
			{
				Directory.CreateDirectory(destDir);
			}

			// Copy using Stream for Async/Cancellation support and Retry logic
			await _retryPolicy.ExecuteAsync(async () =>
			{
				using (FileStream sourceStream =
				       new(stagingPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
				using (FileStream destStream = new(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096,
					       true))
				{
					await sourceStream.CopyToAsync(destStream, ct);
				}

				return true;
			}, ct);

            // Delete staging file after successful copy to emulate moving semantics.
			// If staging and target are the same path, do not delete.
			try
			{
				string stagingFull = Path.GetFullPath(stagingPath);
				string destFull = Path.GetFullPath(targetPath);
				if (!string.Equals(stagingFull, destFull, StringComparison.OrdinalIgnoreCase) && File.Exists(stagingFull))
				{
					try
					{
						File.Delete(stagingFull);
					}
					catch
					{
						// best-effort: do not fail the transfer if we cannot delete staging
					}
				}
			}
			catch
			{
				// ignore any errors when attempting to resolve full paths or delete
			}

			return OperationResult.Ok();
		}
		catch (Exception ex)
		{
			return OperationResult.Fail(ex.Message);
		}
	}
}