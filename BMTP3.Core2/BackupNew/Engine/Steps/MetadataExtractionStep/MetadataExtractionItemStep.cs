using BMTP3.Core2.BackupNew.Api.Progress.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;

namespace BMTP3.Core2.BackupNew.Engine.Steps.MetadataExtractionStep;

/// <summary>
///     Step for extracting deep metadata (EXIF, XMP, IPTC) from the file content.
/// </summary>
public class MetadataExtractionItemStep : IBackupItemStep<BackupPlan, bool>
{
	private readonly IMetadataReader _extractor;

	public MetadataExtractionItemStep(IMetadataReader extractor, BackupPlan context)
	{
		_extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
		Context = context ?? throw new ArgumentNullException(nameof(context));
	}

   public string Name => "Metadata Extraction";
   public FilePhase Phase => FilePhase.None; // No direct match in new model

	public BackupPlan Context { get; }

	public async Task<bool> ExecuteAsync(IBackupItem item, IProgress<ulong> progress, CancellationToken ct)
	{
		await _extractor.EnrichMetadataAsync(item, ct);
		return true;
	}
}