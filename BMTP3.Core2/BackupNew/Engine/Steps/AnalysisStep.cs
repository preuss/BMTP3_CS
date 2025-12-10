using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;

namespace BMTP3.Core2.BackupNew.Engine.Steps;

public class AnalysisStep : IBackupStep
{
    private readonly IMetadataExtractor _metadataExtractor;

    public AnalysisStep(IMetadataExtractor metadataExtractor)
    {
        _metadataExtractor = metadataExtractor;
    }

    public string Name => "Analysis";

    public async Task ExecuteAsync(IBackupItem item, BackupPlan plan, CancellationToken ct)
    {
        try
        {
            // Extract dates and basic info
            await _metadataExtractor.EnrichMetadataAsync(item, ct);

            // Note: We do NOT compute Hash here eagerly. 
            // We let CollisionResolver do it lazily if needed, 
            // OR we could do it here if we know we always need it (e.g. for Indexing).
            // For now, lazy is better for performance.
            
            // Advance state (conceptually, though state is managed by engine/item)
            // item.State = BackupState.Analyzed; // If we had a setter
        }
        catch (Exception ex)
        {
            item.Fail($"Analysis failed: {ex.Message}", Name, ex);
        }
    }
}