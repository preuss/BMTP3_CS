using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
/// Applies the timestamp waterfall logic to a backup item:
/// EXIF &gt; MTP &gt; FileSystem Created &gt; FileSystem Modified &gt; UTC Now.
/// </summary>
public interface ITimestampWaterfall
{
    /// <summary>
    /// Selects the best available timestamp from the item's metadata and stores
    /// the result in <see cref="MetadataKey.AuthoredDateTime"/> and
    /// <see cref="MetadataKey.TimestampSource"/>.
    /// </summary>
    void Apply(IBackupItem item);
}
