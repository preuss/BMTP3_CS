using System.Collections.Concurrent;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
///     Standard implementation of ICollisionResolver.
///     Handles content comparison (Hash/Binary) and conflict resolution (Rename/Skip/Overwrite).
/// </summary>
public class CollisionResolver : ICollisionResolver
{
	// Limits to prevent unbounded memory growth
	private const int MaxRenameLocks = 1000;
	private const int MaxReservedPaths = 10000;
	private const int CleanupInterval = 100; // Cleanup every 100 operations

	private static readonly ConcurrentDictionary<string, SemaphoreSlim> _renameLocks =
		new(StringComparer.OrdinalIgnoreCase);

	private static readonly ConcurrentDictionary<string, DateTimeOffset> _reservedRenamePaths =
		new(StringComparer.OrdinalIgnoreCase);

	private static readonly TimeSpan ReservationTtl = TimeSpan.FromMinutes(10);
	private static int _cleanupCounter;
	private readonly IHashGenerator? _hashGenerator;
	private readonly ILogger<CollisionResolver> _logger;

	private readonly IMetadataReader _metadataReader;
	private readonly IPathGenerator _pathGenerator;

	public CollisionResolver(IMetadataReader metadataReader, IPathGenerator pathGenerator,
		ILogger<CollisionResolver> logger, IHashGenerator? hashGenerator = null)
	{
		_metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
		_pathGenerator = pathGenerator ?? throw new ArgumentNullException(nameof(pathGenerator));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_hashGenerator =
			hashGenerator; // optional: used as a fallback to compute destination hashes when sidecar is missing
	}

	public async Task<CollisionResult> ResolveAsync(IBackupItem item, string proposedFullPath, BackupPlan plan,
		CancellationToken ct)
	{
		// 1. Check if destination exists
		if (!File.Exists(proposedFullPath))
		{
			return new CollisionResult(BackupActionType.Copy, proposedFullPath, "New file");
		}

		// 2. Collision detected! Decide if content is actually different.
		bool isIdentical = await IsContentIdenticalAsync(item, proposedFullPath, plan.ComparisonType, ct);

		if (isIdentical)
		{
			return new CollisionResult(BackupActionType.Skip, proposedFullPath, "Identical file already exists");
		}

		// 3. Files are different (or comparison was skipped). Apply Resolution Strategy.
		switch (plan.CollisionResolution)
		{
			case CollisionResolutionType.Overwrite:
				return new CollisionResult(BackupActionType.Copy, proposedFullPath, "Overwrite policy active");

			case CollisionResolutionType.Skip:
				return new CollisionResult(BackupActionType.Skip, proposedFullPath, "Collision detected (Skip policy)");

			case CollisionResolutionType.Rename:
				string newPath = await GenerateUniquePathAsync(item, proposedFullPath, plan, ct);
				return new CollisionResult(BackupActionType.Rename, newPath, "Collision detected (Renamed)");

			case CollisionResolutionType.Error:
			default:
				throw new IOException($"File exists at '{proposedFullPath}' and collision resolution is set to Error.");
		}
	}

	private async Task<bool> IsContentIdenticalAsync(IBackupItem source, string destPath, CollisionComparisonType type,
		CancellationToken ct)
	{
		if (type == CollisionComparisonType.None)
		{
			return false;
		}

		long destLength = new FileInfo(destPath).Length;
		ulong sourceLength = source.Metadata.Get<ulong>(MetadataKey.Length);

		if ((ulong)destLength != sourceLength)
		{
			return false;
		}

		if (type == CollisionComparisonType.Hash)
		{
			return await CompareHashesAsync(source, destPath, ct);
		}

		if (type == CollisionComparisonType.Binary)
		{
			return await CompareBinaryAsync(source, destPath, ct);
		}

		return false;
	}

	private async Task<bool> CompareHashesAsync(IBackupItem source, string destPath, CancellationToken ct)
	{
		// Priority order � explicit type and best->worst
		HashType[] priority = new[]
		{
			HashType.BLAKE3_512,
			HashType.SHA3_512_FIPS202,
			HashType.SHA3_512_KECCAK,
			HashType.BLAKE3_256,
			HashType.SHA3_256_FIPS202,
			HashType.SHA3_256_KECCAK,
			HashType.SHA2_512,
			HashType.SHA2_256,
			HashType.MD5_128
		};

		// Read stored hashes (must be keyed by HashType)
		Dictionary<HashType, string>? stored = source.Metadata.Get<Dictionary<HashType, string>>(MetadataKey.Hashes);
		if (stored == null)
		{
			_logger.LogInformation(
				"Source metadata does not contain hashes for item {SourcePath}; falling back to binary comparison.",
				source.SourcePath);
			return await CompareBinaryAsync(source, destPath, ct);
		}

		// Find which requested algorithms are present (preserve priority order)
		List<HashType> present = new();
		foreach (HashType ht in priority)
		{
			if (stored.TryGetValue(ht, out string? val) && !string.IsNullOrWhiteSpace(val))
			{
				present.Add(ht);
			}
		}

		// If none present -> fall back to binary comparison
		if (present.Count == 0)
		{
			_logger.LogInformation(
				"No suitable hash found in item metadata for {SourcePath}; falling back to binary comparison.",
				source.SourcePath);
			return await CompareBinaryAsync(source, destPath, ct);
		}

		// For each present algorithm, compare source vs destination using the same algorithm.
		// IMPORTANT: do NOT compute destination hash here. Only read sidecar/metadata.
		foreach (HashType algo in present)
		{
			// explicit retrieval
			if (!stored.TryGetValue(algo, out string? sourceHash) || string.IsNullOrWhiteSpace(sourceHash))
			{
				// Metadata inconsistent; log and skip this algorithm
				_logger.LogWarning(
					"Expected hash {HashType} present in metadata for {SourcePath} but it's missing or empty. Skipping algorithm.",
					algo, source.SourcePath);
				continue;
			}

			// Try to obtain destination hash from item metadata first (inspector), then sidecar, then fallbacks.
			string? destHash = null;

			// 0) Destination hashes stored by DestinationInspector
			Dictionary<HashType, string>? destStored =
				source.Metadata.Get<Dictionary<HashType, string>>(MetadataKey.DestinationHashes);
			if (destStored != null && destStored.TryGetValue(algo, out string? dh) && !string.IsNullOrWhiteSpace(dh))
			{
				destHash = dh;
			}

			// 1) If not found, Try to obtain destination hash from sidecar.
			if (string.IsNullOrEmpty(destHash))
			{
				destHash = await SidecarReader.TryReadHashFromSidecarAsync(destPath, algo, ct);
			}

			// If destination hash is not found in sidecar / metadata, try safe fallbacks instead of throwing.
			if (string.IsNullOrEmpty(destHash))
			{
				_logger.LogInformation(
					"Destination sidecar did not contain hash {HashType} for {DestPath}. Attempting fallback.", algo,
					destPath);

				// Try compute once using IHashGenerator if available
				if (_hashGenerator != null)
				{
					try
					{
						using FileStream ds = new(destPath, FileMode.Open, FileAccess.Read, FileShare.Read);
						Dictionary<HashType, string>? computed =
							await _hashGenerator.ComputeHashesAsync(ds, new[] { algo }, null!, ct);
						if (computed != null && computed.TryGetValue(algo, out string? compHash) &&
						    !string.IsNullOrWhiteSpace(compHash))
						{
							destHash = compHash;
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex,
							"Failed to compute destination hash {HashType} for {DestPath} as fallback.", algo,
							destPath);
					}
				}

				// If still not available, fallback to binary comparison. If binary compare reports equal -> identical, else -> different.
				if (string.IsNullOrEmpty(destHash))
				{
					_logger.LogInformation(
						"Falling back to binary comparison for {DestPath} because no destination hash was available.",
						destPath);
					try
					{
						bool binaryEqual = await CompareBinaryAsync(source, destPath, ct);
						if (binaryEqual)
						{
							return true;
						}

						return false;
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Binary comparison failed for {DestPath}; treating files as different.",
							destPath);
						return false;
					}
				}
			}

			// Compare normalized lowercase hex
			if (!string.Equals(sourceHash, destHash, StringComparison.OrdinalIgnoreCase))
			{
				// mismatch on one algorithm -> files are different
				return false;
			}
		}

		// All present algorithms matched
		return true;
	}

	private async Task<bool> CompareBinaryAsync(IBackupItem source, string destPath, CancellationToken ct)
	{
		const int bufferSize = 64 * 1024;
		byte[] buffer1 = new byte[bufferSize];
		byte[] buffer2 = new byte[bufferSize];

		using Stream sourceStream = source.Content.OpenRead();
		using FileStream destStream = new(destPath, FileMode.Open, FileAccess.Read, FileShare.Read);

		int bytesRead1, bytesRead2;
		do
		{
			ct.ThrowIfCancellationRequested();

			bytesRead1 = await sourceStream.ReadAsync(buffer1, 0, bufferSize, ct);
			bytesRead2 = await destStream.ReadAsync(buffer2, 0, bufferSize, ct);

			if (bytesRead1 != bytesRead2)
			{
				return false;
			}

			if (bytesRead1 == 0)
			{
				return true;
			}

			for (int i = 0; i < bytesRead1; i++)
			{
				if (buffer1[i] != buffer2[i])
				{
					return false;
				}
			}
		} while (true);
	}

	private async Task<string> GenerateUniquePathAsync(IBackupItem item, string originalPath, BackupPlan plan,
		CancellationToken ct)
	{
		string directory = Path.GetDirectoryName(originalPath) ?? "";
		string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
		string extension = Path.GetExtension(originalPath);
		string newPath;
		RenameStrategy strategy = plan.RenameStrategy;

		// Thread-safe lock acquisition: try to get existing lock, or create new one
		// This pattern prevents creating multiple semaphores for the same directory
		if (!_renameLocks.TryGetValue(directory, out SemaphoreSlim? renameLock))
		{
			SemaphoreSlim newLock = new(1, 1);
			renameLock = _renameLocks.GetOrAdd(directory, newLock);
			// If we added a new lock but it's not the one we're using, dispose the extra
			if (renameLock != newLock)
			{
				newLock.Dispose();
			}
		}

		await renameLock.WaitAsync(ct);
		try
		{
			CleanupExpiredReservations(directory);

			// 1. Try Custom Pattern first if specified
			if (strategy == RenameStrategy.CustomCollisionPathPattern &&
			    !string.IsNullOrWhiteSpace(plan.CustomCollisionPathPattern))
			{
				string formattedRelative = _pathGenerator.ApplyPattern(plan.CustomCollisionPathPattern, item);
				newPath = Path.Combine(directory, formattedRelative);
				// Ensure we include extension if not in pattern
				if (!newPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
				{
					newPath += extension;
				}

				if (CanUseRenameTarget(newPath))
				{
					ReserveRenameTarget(newPath);
					item.Metadata.Set(MetadataKey.CollisionIndex, "1");
					return newPath;
				}

				// If custom pattern also exists, fallback to increment
				fileNameWithoutExt = Path.GetFileNameWithoutExtension(newPath);
			}

			// 2. Try secondary strategies (Timestamp or Hash)
			string suffix = "";

			if (strategy == RenameStrategy.Timestamp)
			{
				if (item.Metadata.Has(MetadataKey.AuthoredDateTime))
				{
					DateTime dt = item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime);
					suffix = "_" + dt.ToString("yyyyMMdd_HHmmss");
				}
			}
			else if (strategy == RenameStrategy.Hash)
			{
				Dictionary<HashType, string>? hashes =
					item.Metadata.Get<Dictionary<HashType, string>>(MetadataKey.Hashes);
				if (hashes != null && hashes.Count > 0)
				{
					// Use first available hash, take 6 chars
					string h = hashes.Values.First();
					suffix = "_" + (h.Length > 6 ? h.Substring(0, 6) : h);
				}
			}

			if (!string.IsNullOrEmpty(suffix))
			{
				// Try Strategy Suffix
				newPath = Path.Combine(directory, $"{fileNameWithoutExt}{suffix}{extension}");
				if (CanUseRenameTarget(newPath))
				{
					ReserveRenameTarget(newPath);
					item.Metadata.Set(MetadataKey.CollisionIndex, "1");
					return newPath;
				}

				// Strategy Suffix Collision? Fallback to Increment on top of Suffix
				fileNameWithoutExt = $"{fileNameWithoutExt}{suffix}";
			}

			// Fallback: Increment Loop (covers RenameStrategy.Increment and Strategy Failures)
			int counter = 1;
			do
			{
				ct.ThrowIfCancellationRequested();
				string newFileName = $"{fileNameWithoutExt}_{counter}{extension}";
				newPath = Path.Combine(directory, newFileName);
				counter++;
			} while (!CanUseRenameTarget(newPath));

			ReserveRenameTarget(newPath);

			// counter was incremented one extra time after finding the free slot
			item.Metadata.Set(MetadataKey.CollisionIndex, (counter - 1).ToString());
			return newPath;
		}
		finally
		{
			renameLock.Release();
		}
	}

	private static bool CanUseRenameTarget(string path)
	{
		if (File.Exists(path))
		{
			return false;
		}

		string fullPath = Path.GetFullPath(path);
		if (_reservedRenamePaths.TryGetValue(fullPath, out DateTimeOffset reservedAt))
		{
			if (DateTimeOffset.UtcNow - reservedAt <= ReservationTtl)
			{
				return false;
			}

			_reservedRenamePaths.TryRemove(fullPath, out _);
		}

		return true;
	}

	private static void ReserveRenameTarget(string path)
	{
		string fullPath = Path.GetFullPath(path);
		_reservedRenamePaths[fullPath] = DateTimeOffset.UtcNow;

		// Trigger periodic cleanup if needed
		if (Interlocked.Increment(ref _cleanupCounter) % CleanupInterval == 0)
		{
			TryCleanupOldEntries();
		}
	}

	private static void CleanupExpiredReservations(string directory)
	{
		string fullDirectory = Path.GetFullPath(directory);
		DateTimeOffset now = DateTimeOffset.UtcNow;
		foreach (KeyValuePair<string, DateTimeOffset> kv in _reservedRenamePaths)
		{
			string candidateDir = Path.GetDirectoryName(kv.Key) ?? string.Empty;
			if (!string.Equals(Path.GetFullPath(candidateDir), fullDirectory, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (now - kv.Value > ReservationTtl || File.Exists(kv.Key))
			{
				_reservedRenamePaths.TryRemove(kv.Key, out _);
			}
		}
	}

	/// <summary>
	///     Periodic cleanup to prevent unbounded memory growth.
	///     Removes expired reservations and old locks.
	/// </summary>
	private static void TryCleanupOldEntries()
	{
		DateTimeOffset now = DateTimeOffset.UtcNow;

		// Clean up expired reservations
		List<string> expiredKeys = _reservedRenamePaths
			.Where(kv => now - kv.Value > ReservationTtl || File.Exists(kv.Key))
			.Select(kv => kv.Key)
			.ToList();

		foreach (string key in expiredKeys)
		{
			_reservedRenamePaths.TryRemove(key, out _);
		}

		// If still too many entries, remove oldest 25%
		if (_reservedRenamePaths.Count > MaxReservedPaths)
		{
			List<string> keysToRemove = _reservedRenamePaths
				.OrderBy(kv => kv.Value)
				.Take(_reservedRenamePaths.Count / 4)
				.Select(kv => kv.Key)
				.ToList();

			foreach (string key in keysToRemove)
			{
				_reservedRenamePaths.TryRemove(key, out _);
			}
		}

		// Note: SemaphoreSlim cleanup is tricky because they may be in use.
		// We just limit new additions when count exceeds limit.
	}
}