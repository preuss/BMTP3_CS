using System.Security.Cryptography;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;

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
        string sourceHash = source.Metadata.Get<string>(MetadataKey.HashSha256);
        if (string.IsNullOrEmpty(sourceHash))
        {
            sourceHash = await _metadataExtractor.ComputeHashAsync(source, ct);
            source.Metadata.Set(MetadataKey.HashSha256, sourceHash);
        }

        string destHash = await ComputeFileHashAsync(destPath, ct);

        return string.Equals(sourceHash, destHash, StringComparison.OrdinalIgnoreCase);
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

    private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        
        byte[] hashBytes = await sha256.ComputeHashAsync(stream, ct);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
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
