using System.Security.Cryptography;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
/// Standard implementation of ICollisionResolver.
/// Handles content comparison (Hash/Binary) and conflict resolution (Rename/Skip/Overwrite).
/// </summary>
public class CollisionResolver : ICollisionResolver
{
	private readonly IMetadataExtractor _metadataExtractor;

	public CollisionResolver(IMetadataExtractor metadataExtractor)
	{
		_metadataExtractor = metadataExtractor ?? throw new ArgumentNullException(nameof(metadataExtractor));
	}

	public async Task<CollisionResult> ResolveAsync(IBackupItem item, string proposedFullPath, BackupPlan plan, CancellationToken ct)
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
				string newPath = await GenerateUniquePathAsync(proposedFullPath, plan.RenameStrategy, ct);
				return new CollisionResult(BackupActionType.Rename, newPath, "Collision detected (Renamed)");

			case CollisionResolutionType.Error:
			default:
				throw new IOException($"File exists at '{proposedFullPath}' and collision resolution is set to Error.");
		}
	}

	private async Task<bool> IsContentIdenticalAsync(IBackupItem source, string destPath, CollisionComparisonType type, CancellationToken ct)
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
		// Priority order — explicit type and best->worst
		HashType[] priority = new HashType[]
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
			throw new InvalidOperationException("Source item metadata does not contain any hashes. Ensure hashing ran before collision resolution.");
		}

		// Find which requested algorithms are present (preserve priority order)
		List<HashType> present = new List<HashType>();
		foreach (HashType ht in priority)
		{
			if (stored.TryGetValue(ht, out string? val) && !string.IsNullOrWhiteSpace(val))
			{
				present.Add(ht);
			}
		}

		// If none present -> cannot compare by hash
		if (present.Count == 0)
		{
			throw new InvalidOperationException("No suitable hash found in item metadata. Cannot perform hash-based comparison.");
		}

		// For each present algorithm, compare source vs destination using the same algorithm.
		// IMPORTANT: do NOT compute destination hash here. Only read sidecar/metadata.
		foreach (HashType algo in present)
		{
			// explicit retrieval
			if (!stored.TryGetValue(algo, out string? sourceHash) || string.IsNullOrWhiteSpace(sourceHash))
			{
				throw new InvalidOperationException($"Hash for {algo} is missing despite earlier detection. Metadata inconsistent.");
			}

			// Try to obtain destination hash from sidecar ONLY.
			string? destHash = await TryReadHashFromSidecarAsync(destPath, algo, ct);

			// If destination hash is not found in sidecar / metadata, fail fast:
			// orchestration must have prepared destination facts (sidecar or inspector) before calling resolver.
			if (string.IsNullOrEmpty(destHash))
			{
				throw new InvalidOperationException($"Destination hash for {algo} not available for '{destPath}'. Ensure destination sidecar or inspector prepared hashes before collision resolution.");
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

	private static async Task<string?> TryReadHashFromSidecarAsync(string destPath, HashType algorithm, CancellationToken ct)
	{
		string[] candidates = new string[]
		{
			destPath + ".bmtp3.json",
			destPath + ".meta.json",
			destPath + ".metadata.json",
			destPath + ".json",
			Path.ChangeExtension(destPath, ".meta"),
			Path.ChangeExtension(destPath, ".ini")
		};

		foreach (string sidecar in candidates)
		{
			if (!File.Exists(sidecar)) continue;

			ct.ThrowIfCancellationRequested();

			try
			{
				string text = await File.ReadAllTextAsync(sidecar, ct);
				if (string.IsNullOrWhiteSpace(text)) continue;

				using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(text);
				System.Text.Json.JsonElement root = doc.RootElement;

				if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
				{
					// try hashes object
					if (root.TryGetProperty("hashes", out System.Text.Json.JsonElement hashesEl) && hashesEl.ValueKind == System.Text.Json.JsonValueKind.Object)
					{
						// try algorithm name key
						if (hashesEl.TryGetProperty(algorithm.ToString(), out System.Text.Json.JsonElement algoEl) && algoEl.ValueKind == System.Text.Json.JsonValueKind.String)
						{
							return algoEl.GetString();
						}

						// legacy SHA256 key for SHA2_256
						if (algorithm == HashType.SHA2_256 && hashesEl.TryGetProperty("SHA256", out System.Text.Json.JsonElement shaEl) && shaEl.ValueKind == System.Text.Json.JsonValueKind.String)
						{
							return shaEl.GetString();
						}
					}

					// direct key fallback
					if (root.TryGetProperty(algorithm.ToString(), out System.Text.Json.JsonElement directEnum) && directEnum.ValueKind == System.Text.Json.JsonValueKind.String)
					{
						return directEnum.GetString();
					}

					if (algorithm == HashType.SHA2_256 && root.TryGetProperty("SHA256", out System.Text.Json.JsonElement direct) && direct.ValueKind == System.Text.Json.JsonValueKind.String)
					{
						return direct.GetString();
					}
				}
			}
			catch
			{
				// ignore and try next
			}

			// try simple key=value lines
			try
			{
				foreach (string line in File.ReadLines(sidecar))
				{
					ct.ThrowIfCancellationRequested();

					int idx = line.IndexOf('=');
					if (idx <= 0) continue;
					string key = line.Substring(0, idx).Trim();
					string val = line.Substring(idx + 1).Trim();
					if (string.Equals(key, algorithm.ToString(), StringComparison.OrdinalIgnoreCase) || (algorithm == HashType.SHA2_256 && string.Equals(key, "SHA256", StringComparison.OrdinalIgnoreCase)))
					{
						if (!string.IsNullOrWhiteSpace(val)) return val;
					}
				}
			}
			catch
			{
				// ignore
			}
		}

		return null;
	}

	private async Task<bool> CompareBinaryAsync(IBackupItem source, string destPath, CancellationToken ct)
	{
		const int bufferSize = 64 * 1024;
		byte[] buffer1 = new byte[bufferSize];
		byte[] buffer2 = new byte[bufferSize];

		using var sourceStream = source.Content.OpenRead();
		using var destStream = new FileStream(destPath, FileMode.Open, FileAccess.Read, FileShare.Read);

		int bytesRead1, bytesRead2;
		do
		{
			ct.ThrowIfCancellationRequested();

			bytesRead1 = await sourceStream.ReadAsync(buffer1, 0, bufferSize, ct);
			bytesRead2 = await destStream.ReadAsync(buffer2, 0, bufferSize, ct);

			if (bytesRead1 != bytesRead2) return false;
			if (bytesRead1 == 0) return true;

			for (int i = 0; i < bytesRead1; i++)
			{
				if (buffer1[i] != buffer2[i]) return false;
			}

		} while (true);
	}

	private Task<string> GenerateUniquePathAsync(string originalPath, RenameStrategy strategy, CancellationToken ct)
	{
		string directory = Path.GetDirectoryName(originalPath) ?? "";
		string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
		string extension = Path.GetExtension(originalPath);

		int counter = 1;
		string newPath;

		do
		{
			ct.ThrowIfCancellationRequested();

			string newFileName = $"{fileNameWithoutExt}_{counter}{extension}";
			newPath = Path.Combine(directory, newFileName);
			counter++;

		} while (File.Exists(newPath));

		return Task.FromResult(newPath);
	}
}
