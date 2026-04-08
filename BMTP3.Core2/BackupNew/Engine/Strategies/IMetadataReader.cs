using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
///     Responsible for extracting metadata from source items.
/// </summary>
public interface IMetadataReader
{
	/// <summary>
	///     Extracts essential metadata (dates, size) needed for path generation and filtering.
	///     Does NOT necessarily calculate hashes (that's heavy and should be lazy or separate).
	/// </summary>
	Task EnrichMetadataAsync(IBackupItem item, CancellationToken ct);
}