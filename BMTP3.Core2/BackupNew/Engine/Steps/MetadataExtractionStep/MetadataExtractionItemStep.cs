using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps.MetadataExtractionStep;

/// <summary>
/// Step for extracting deep metadata (EXIF, XMP, IPTC) from the file content.
/// </summary>
public class MetadataExtractionItemStep : IBackupItemStep<BackupPlan, bool>
{
    	private readonly IMetadataReader _extractor;
    
    	public string Name => "Metadata Extraction";
        public FilePhase Phase => FilePhase.Metadata;
    
    	public BackupPlan Context { get; private set; }
    
    	public MetadataExtractionItemStep(IMetadataReader extractor, BackupPlan context)
    	{
    		_extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
    		Context = context ?? throw new ArgumentNullException(nameof(context));
    	}
    public async Task<bool> ExecuteAsync(IBackupItem item, CancellationToken ct)
    {
        await _extractor.EnrichMetadataAsync(item, ct);
        return true;
    }

    public async Task<bool> ExecuteAsync(BackupPlan plan, IBackupItem item, CancellationToken ct)
    {
        Context = plan ?? throw new ArgumentNullException(nameof(plan));
        await _extractor.EnrichMetadataAsync(item, ct);
        return true;
    }
}
