using System.Security.Cryptography;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Content;
using System.Collections.Generic;
using BMTP3.Core2.BackupNew.Engine.HashGenerator;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
/// Standard implementation of IMetadataExtractor.
/// Extracts dates and calculates hashes from BackupItems.
/// </summary>
public class MetadataExtractor : IMetadataExtractor
{
    public Task EnrichMetadataAsync(IBackupItem item, CancellationToken ct)
    {
        // 1. Ensure basic file info is present (Length is usually set at creation)
        if (!item.Metadata.Has(MetadataKey.Length))
        {
            item.Metadata.Set(MetadataKey.Length, item.Content.Length);
        }

        // 2. Extract Dates
        if (!item.Metadata.Has(MetadataKey.AuthoredDateTime))
        {
            if (item.Metadata.Has(MetadataKey.ModifiedDateTime))
            {
                var modDate = item.Metadata.Get<DateTime>(MetadataKey.ModifiedDateTime);
                item.Metadata.Set(MetadataKey.AuthoredDateTime, modDate);
            }
            else if (item.Metadata.Has(MetadataKey.CreatedDateTime))
            {
                 var createDate = item.Metadata.Get<DateTime>(MetadataKey.CreatedDateTime);
                 item.Metadata.Set(MetadataKey.AuthoredDateTime, createDate);
            }
            else
            {
                item.Metadata.Set(MetadataKey.AuthoredDateTime, DateTime.Now);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Computes SHA-256 hash for the item and stores it in metadata under the "hashes" dictionary.
    /// Returns hex-encoded lowercase string of the SHA-256 hash.
    /// </summary>
    public async Task<Dictionary<HashType, string>> ComputeHashAsync(IBackupItem item, IEnumerable<HashType> algorithms, CancellationToken ct)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));

        var requested = (algorithms ?? Enumerable.Empty<HashType>()).Distinct().ToArray();
        if (requested.Length == 0)
            requested = new[] { HashType.SHA2_256 };

        // read existing hashes from metadata (stored as Dictionary<string,string>)
        var stored = item.Metadata.Get<Dictionary<string, string>>(MetadataKey.Hashes) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<HashType, string>();

        // Determine which algorithms we need to compute
        var missing = requested.Where(a => !stored.TryGetValue(a.ToString(), out var v) || string.IsNullOrEmpty(v)).Distinct().ToArray();

        if (missing.Length > 0)
        {
            // Currently only SHA2_256 is implemented
            if (missing.Any(a => a != HashType.SHA2_256))
            {
                throw new NotSupportedException("Only SHA2_256 is supported by this MetadataExtractor implementation.");
            }

            // Compute SHA256 once
            using var sha256 = SHA256.Create();
            using var stream = item.Content.OpenRead();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, ct);
            string hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

            stored[HashType.SHA2_256.ToString()] = hashString;
        }

        // Persist updated hashes dictionary back to metadata
        if (missing.Length > 0)
        {
            item.Metadata.Set(MetadataKey.Hashes, stored);
        }

        // Build result map from stored values
        foreach (var algo in requested)
        {
            if (stored.TryGetValue(algo.ToString(), out var val) && !string.IsNullOrEmpty(val))
            {
                result[algo] = val;
            }
            else
            {
                // Should not happen because we computed missing above, but guard anyway
                throw new InvalidOperationException($"Requested hash {algo} is not available after computation.");
            }
        }

        return result;
    }
}
