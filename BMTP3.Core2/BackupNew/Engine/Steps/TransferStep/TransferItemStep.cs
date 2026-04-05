using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Models;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Engine.Transfers;
using System.IO;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Steps.TransferStep;

public class TransferItemStep : IBackupItemStep<BackupPlan, OperationResult>
{
    private readonly IPathGenerator _pathGenerator;
    private readonly ICollisionResolver _collisionResolver;
    private readonly IFileTransfer _fileTransfer;
    private readonly IItemHasher _itemHasher;
    private readonly Microsoft.Extensions.Logging.ILogger<TransferItemStep>? _logger;

	public string Name => "Transfer";
	public FilePhase Phase => FilePhase.Transferring;

    private readonly BackupPlan _context;
    public BackupPlan Context => _context;

    public TransferItemStep(
        BackupPlan context,
        IPathGenerator pathGenerator,
        ICollisionResolver collisionResolver,
        IFileTransfer fileTransfer,
        IItemHasher itemHasher,
        Microsoft.Extensions.Logging.ILogger<TransferItemStep>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _pathGenerator = pathGenerator ?? throw new ArgumentNullException(nameof(pathGenerator));
        _collisionResolver = collisionResolver ?? throw new ArgumentNullException(nameof(collisionResolver));
        _fileTransfer = fileTransfer ?? throw new ArgumentNullException(nameof(fileTransfer));
        _itemHasher = itemHasher ?? throw new ArgumentNullException(nameof(itemHasher));
        _logger = logger;
    }

    private async Task<bool> CompareBinaryAsync(IBackupItem source, string destPath, CancellationToken ct)
    {
        const int bufferSize = 64 * 1024;
        byte[] buffer1 = new byte[bufferSize];
        byte[] buffer2 = new byte[bufferSize];

        // Add basic retry/backoff for IO errors (file locked etc.)
        int attempts = 3;
        int backoffMs = 50;
			for(int attempt = 1; attempt <= attempts; attempt++)
			{
				try
            {
                using Stream sourceStream = source.Content.OpenRead();
                using FileStream destStream = new(destPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                int bytesRead1, bytesRead2;
                do
                {
                    ct.ThrowIfCancellationRequested();

                    bytesRead1 = await sourceStream.ReadAsync(buffer1, 0, bufferSize, ct);
                    bytesRead2 = await destStream.ReadAsync(buffer2, 0, bufferSize, ct);

                    if(bytesRead1 != bytesRead2) return false;
                    if(bytesRead1 == 0) return true;

                    for(int i = 0; i < bytesRead1; i++)
                    {
                        if(buffer1[i] != buffer2[i]) return false;
                    }

                } while(true);
            }
            catch(IOException) when (attempt < attempts)
            {
                try { await Task.Delay(backoffMs * attempt, ct); } catch { }
                continue;
            }
        }

        return false;
    }

	public async Task<OperationResult> ExecuteAsync(IBackupItem item, IProgress<ulong> progress, CancellationToken ct)
	{
		// 1. Determine relative path
		string relativePath = _pathGenerator.GenerateRelativePath(item, _context);
		string destinationPath = Path.Combine(_context.OutputPath, relativePath);

		// 2. Resolve collisions
		CollisionResult collision = await _collisionResolver.ResolveAsync(item, destinationPath, _context, ct);

		// 3. Act based on collision result
		if(collision.Action == BackupActionType.Skip)
		{
			item.SetResult(ItemResultState.Skipped, collision.Reason);
			item.AddLog($"Skipped: {collision.Reason}", Name);
			return OperationResult.Skipped(collision.Reason);
		}

		if(collision.Action == BackupActionType.Rename)
		{
			destinationPath = collision.TargetPath; // Use the new unique path
			item.AddLog($"Renamed to: {Path.GetFileName(destinationPath)}", Name);
		}

		// 4. Transfer
		// Ensure we have a local file to transfer
		if(item.Content is not FileContent fileContent)
		{
			string msg = $"Content is not a local file (found {item.Content?.GetType().Name}). Staging step might have failed.";
			item.Fail(msg, Name);
			return OperationResult.Fail(msg);
		}

		// Ensure directory exists
		string? destDir = Path.GetDirectoryName(destinationPath);
		if(!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir) && !_context.DryRun)
		{
			Directory.CreateDirectory(destDir);
		}

        // (diagnostic logging removed)

        // If staging path == destination path, avoid calling the transfer (File.Copy would throw)
        OperationResult result;
        try
        {
            var stagingFull = Path.GetFullPath(fileContent.FileInfo.FullName);
            var destFull = Path.GetFullPath(destinationPath);
            if(string.Equals(stagingFull, destFull, StringComparison.OrdinalIgnoreCase))
            {
                // No-op transfer (already at destination)
                result = OperationResult.Ok();
            }
            else
            {
                result = await _fileTransfer.TransferAsync(fileContent.FileInfo.FullName, destinationPath, _context.DryRun, ct);
            }
        }
        catch(Exception ex)
        {
            // Ensure we surface the transfer failure as OperationResult
            result = OperationResult.Fail(ex.Message);
        }

        // Log the raw transfer result regardless of success/failure so we can debug test runs
            // (diagnostic logging removed)

		if(result.Success)
		{
			if(_context.DryRun)
			{
				TryCleanupDryRunStaging(item);
			}

			// BUG-06 FIX: LocalFileTransfer deletes the staging file before returning.
			// item.Content still points to the (now-deleted) staging path at this point.
			// Replace it with a FileContent pointing to the destination so that
			// VerifyTransferAsync (hash or binary) opens the correct, existing file.
			// DryRun guard: skip the replace in dry-run because no file was actually written.
			if(!_context.DryRun && File.Exists(destinationPath))
			{
				item.ReplaceContent(new FileContent(destinationPath));
			}
			// Preserve corrected/authored timestamps on destination after the copy.
			if(!_context.DryRun && File.Exists(destinationPath))
			{
				TryApplyDestinationTimestamp(item, destinationPath);
			}

			// 5. Post-Write Verification
			if(!_context.DryRun && _context.PostWriteVerification != Api.Request.Enums.PostWriteVerificationType.None)
			{
				int attempts = Math.Max(1, _context.VerificationRetryCount);
				int delayMs = Math.Max(0, _context.VerificationRetryDelayMs);
				bool verified = false;
		for(int attempt = 1; attempt <= attempts; attempt++)
		{
                // (diagnostic logging removed)
				CancellationTokenSource? linkedCts = null;
					if(_context.VerificationTimeoutMs > 0)
					{
						linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
						linkedCts.CancelAfter(_context.VerificationTimeoutMs);
					}
					try
					{
						verified = await VerifyTransferAsync(item, destinationPath, _context.PostWriteVerification, progress, linkedCts?.Token ?? ct);
						if(verified) break;
					}
					catch(OperationCanceledException) when ((_context.VerificationTimeoutMs > 0) && (linkedCts?.IsCancellationRequested ?? false))
					{
						item.AddLog($"Verification attempt {attempt} timed out.", Name);
					}
					catch(Exception ex)
					{
						item.AddLog($"Verification attempt {attempt} failed: {ex.GetType().Name} {ex.Message}", Name);
					}
					finally
					{
						linkedCts?.Dispose();
					}
					if(!verified && attempt < attempts && delayMs > 0)
					{
						try { await Task.Delay(delayMs, ct); } catch { }
					}
				}

				if(!verified)
				{
					item.Fail("Post-write verification failed. Integrity check mismatch.", Name);
					if(_context.VerificationDeleteOnFailure)
					{
						try { File.Delete(destinationPath); } catch (Exception ex) { item.AddLog($"Failed to delete on verification failure: {ex.Message}", Name); }
					}
					return OperationResult.Fail("Verification failed");
				}

				item.AddLog($"Verified ({_context.PostWriteVerification})", Name);
			}

			item.SetResult(ItemResultState.Success);
			item.Metadata.Set(MetadataKey.FinalTargetPath, destinationPath);
                // (diagnostic logging removed)
        } else
        {
            item.Fail("Transfer failed: " + result.Message, Name);
            // (diagnostic logging removed)
        }

		return result;
	}

	private void TryCleanupDryRunStaging(IBackupItem item)
	{
		if(!item.Metadata.Has(MetadataKey.LocalTempPath))
			return;

		string? tempPath = item.Metadata.Get<string>(MetadataKey.LocalTempPath);
		if(string.IsNullOrWhiteSpace(tempPath))
			return;

		try
		{
			if(File.Exists(tempPath))
			{
				File.Delete(tempPath);
			}

			string? parentDir = Path.GetDirectoryName(tempPath);
			if(!string.IsNullOrWhiteSpace(parentDir) && Directory.Exists(parentDir) && !Directory.EnumerateFileSystemEntries(parentDir).Any())
			{
				Directory.Delete(parentDir);
			}
		}
		catch(Exception ex)
		{
			item.AddLog($"Failed to clean dry-run staging file: {ex.Message}", Name);
		}
	}

	private void TryApplyDestinationTimestamp(IBackupItem item, string destinationPath)
	{
		DateTime? timestamp = null;
		if(item.Metadata.Has(MetadataKey.AuthoredDateTime))
			timestamp = item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime);
		else if(item.Metadata.Has(MetadataKey.CreatedDateTime))
			timestamp = item.Metadata.Get<DateTime>(MetadataKey.CreatedDateTime);
		else if(item.Metadata.Has(MetadataKey.ModifiedDateTime))
			timestamp = item.Metadata.Get<DateTime>(MetadataKey.ModifiedDateTime);

		if(!timestamp.HasValue)
			return;

		try
		{
			DateTime utcTime = timestamp.Value.Kind == DateTimeKind.Unspecified
				? DateTime.SpecifyKind(timestamp.Value, DateTimeKind.Utc)
				: timestamp.Value.ToUniversalTime();

			File.SetLastWriteTimeUtc(destinationPath, utcTime);
			File.SetCreationTimeUtc(destinationPath, utcTime);
		}
		catch(Exception ex)
		{
			item.AddLog($"Failed to apply destination timestamp: {ex.Message}", Name);
		}
	}

    internal async Task<bool> VerifyTransferAsync(IBackupItem item, string destPath, Api.Request.Enums.PostWriteVerificationType type, IProgress<ulong> progress, CancellationToken ct)
    {
        _logger?.LogDebug("VerifyTransferAsync entry: destPath={Dest} type={Type} itemSource={Source}", destPath, type, item.SourcePath);
        // Allow for small wrapper timeout behaviour; callers may pass a linked token with a timeout.
        if(type == Api.Request.Enums.PostWriteVerificationType.Hash)
        {
            // Re-hash destination using the Primary Hash Algo (e.g. BLAKE3 or SHA256)
            // Ensure we have source hashes; if not, compute them on-the-fly using _itemHasher.
            var hashes = item.Metadata.Get<Dictionary<HashType, string>>(MetadataKey.Hashes);
            if(hashes == null || hashes.Count == 0)
            {
                try
                {
                    // Determine which algorithms to request: prefer plan-defined HashTypes if present
                    List<HashType> toCompute = _context.HashTypes?.ToList() ?? new List<HashType> { HashType.SHA2_256 };
                    var computed = await _itemHasher.ComputeHashesAsync(item, toCompute, progress, ct);
                    if(computed != null && computed.Count > 0)
                    {
                        item.Metadata.Set(MetadataKey.Hashes, computed);
                        hashes = computed;
                    }
                }
                catch(Exception ex)
                {
                    item.AddLog($"Hashing source for verification failed: {ex.GetType().Name} {ex.Message}", Name);
                    // Fallback to binary verification if hashing failed
                    _logger?.LogDebug("Source hashing failed: {ExType} {ExMessage}", ex.GetType().Name, ex.Message);
                    return await CompareBinaryAsync(item, destPath, ct);
                }
            }

            if(hashes == null || hashes.Count == 0)
            {
                // Still no hashes -> fallback to binary compare
                return await CompareBinaryAsync(item, destPath, ct);
            }

            // Pick the strongest available hash (first key)
            var algo = hashes.Keys.First();
            string expectedHash = hashes[algo];

            // Compute hash of destination using item hasher by wrapping destination file as a BackupItem
                // When hashing destination, run the hasher but fallback to binary on failure.
                try
                {
                    var destWrapper = BackupItem.Create(new FileContent(new FileInfo(destPath)), Path.GetFileName(destPath));
                    try {
                        string destWrapperPath = "(not file)";
                        if(destWrapper.Content is FileContent df) destWrapperPath = df.FileInfo.FullName;
                        _logger?.LogDebug("DestWrapper path={Path}", destWrapperPath);
                    } catch { }
                    var computedHashes = await _itemHasher.ComputeHashesAsync(destWrapper, new List<HashType> { algo }, progress, ct);

                    if(!computedHashes.TryGetValue(algo, out string? actualHash) || string.IsNullOrWhiteSpace(actualHash))
                    {
                        item.AddLog("Destination hasher did not produce a hash for verification; falling back to binary comparison.", Name);
                        _logger?.LogDebug("Destination hasher produced no hash for algo={Algo}", algo);
                        return await CompareBinaryAsync(item, destPath, ct);
                    }

                    _logger?.LogDebug("VerificationCompare: expected={Expected} actual={Actual} algo={Algo}", expectedHash, actualHash, algo);

                    return string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase);
                }
                catch(Exception ex)
                {
                    item.AddLog($"Hashing destination for verification failed: {ex.GetType().Name} {ex.Message}. Falling back to binary comparison.", Name);
                    return await CompareBinaryAsync(item, destPath, ct);
                }
        } else if(type == Api.Request.Enums.PostWriteVerificationType.Binary)
        {
            return await CompareBinaryAsync(item, destPath, ct);
        }

		return true;
	}
}
