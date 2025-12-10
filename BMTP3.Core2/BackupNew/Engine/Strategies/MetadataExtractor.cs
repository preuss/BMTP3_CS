using System.Security.Cryptography;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Content;

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

    public async Task<string> ComputeHashAsync(IBackupItem item, CancellationToken ct)
    {
        if (item.Metadata.Has(MetadataKey.HashSha256))
        {
            return item.Metadata.Get<string>(MetadataKey.HashSha256)!;
        }

        using var sha256 = SHA256.Create();
        using var stream = item.Content.OpenRead();
        
        byte[] hashBytes = await sha256.ComputeHashAsync(stream, ct);
        string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        item.Metadata.Set(MetadataKey.HashSha256, hashString);

        return hashString;
    }
}
