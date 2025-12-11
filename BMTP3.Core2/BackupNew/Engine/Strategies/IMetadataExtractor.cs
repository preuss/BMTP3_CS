using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.HashGenerator;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
/// Responsible for extracting metadata from source items.
/// </summary>
public interface IMetadataExtractor
{
    /// <summary>
    /// Extracts essential metadata (dates, size) needed for path generation and filtering.
    /// Does NOT necessarily calculate hashes (that's heavy and should be lazy or separate).
    /// </summary>
    Task EnrichMetadataAsync(IBackupItem item, CancellationToken ct);

    /// <summary>
    /// Calculates or retrieves the hash of the item. 
    /// Should only be called if collision detection requires it.
    /// </summary>
    Task<Dictionary<HashType, string>> ComputeHashAsync(IBackupItem item, IEnumerable<HashType> algorithms, CancellationToken ct);
}