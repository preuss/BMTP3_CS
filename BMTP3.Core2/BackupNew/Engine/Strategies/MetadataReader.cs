using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Content;
using System.Collections.Generic;
using Microsoft.Extensions.Logging; // Added for ILogger
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
/// Standard implementation of IMetadataReader.
/// Extracts dates from BackupItems.
/// </summary>
public class MetadataReader : IMetadataReader
{
    private readonly ILogger<MetadataReader> _logger; // Added ILogger

    public MetadataReader(ILogger<MetadataReader> logger) // Constructor updated
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task EnrichMetadataAsync(IBackupItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(item.Content);

        _logger.LogTrace("Extracting metadata for item {itemId} from {itemPath}", item.Id, item.SourcePath);

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
        _logger.LogDebug("Metadata enriched for item {itemId}", item.Id);
        return Task.CompletedTask;
    }
}
