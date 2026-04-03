using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Steps;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BMTP3.Core2.BackupNew.Engine.Steps.InspectorStep;

public class DestinationInspectorItemStep : IBackupItemStep<BackupPlan, object>
{
    private readonly IDestinationInspector _inspector;
    private readonly IHashGenerator _hashGenerator;
    private readonly ILogger<DestinationInspectorItemStep> _logger;
    private readonly BackupPlan _plan;

    public string Name => "DestinationInspector";
    public FilePhase Phase => FilePhase.Hashing; // Reuse Hashing phase for progress semantics

    public BackupPlan Context => _plan;

    public DestinationInspectorItemStep(IDestinationInspector inspector, IHashGenerator hashGenerator, BackupPlan plan, ILogger<DestinationInspectorItemStep> logger)
    {
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        _hashGenerator = hashGenerator ?? throw new ArgumentNullException(nameof(hashGenerator));
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<object> ExecuteAsync(IBackupItem item, IProgress<ulong> progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);

        // If no final target path set, skip.
        if(!item.Metadata.Has(MetadataKey.FinalTargetPath))
        {
            return new object();
        }

        string destPath = item.Metadata.Get<string>(MetadataKey.FinalTargetPath) ?? string.Empty;
        if(string.IsNullOrWhiteSpace(destPath)) return new object();

        try
        {
            // Use IDestinationInspector for a lightweight existence/size check first
            FileSnapshot snapshot = await _inspector.GetSnapshotAsync(destPath, ct);
            if(!snapshot.Exists)
            {
                return new object();
            }

            // Determine algorithms to inspect from plan; fallback to SHA2_256 if empty
            var algorithms = (_plan?.HashTypes != null && _plan.HashTypes.Count > 0)
                ? _plan.HashTypes.ToList()
                : new List<HashType> { HashType.SHA2_256 };

            var found = new Dictionary<HashType, string>();

            // 1) Try to read from destination sidecar
            foreach(var algo in algorithms)
            {
                string? fromSidecar = await SidecarReader.TryReadHashFromSidecarAsync(destPath, algo, ct);
                if(!string.IsNullOrWhiteSpace(fromSidecar))
                {
                    found[algo] = fromSidecar!;
                }
            }

            // 2) For any missing algorithms, use IDestinationInspector (cached, per-path locked)
            var missing = algorithms.Where(a => !found.ContainsKey(a)).ToList();
            if(missing.Count > 0)
            {
                foreach(var algo in missing)
                {
                    try
                    {
                        string algoName = algo.ToString();
                        string hash = await _inspector.GetHashAsync(destPath, algoName, ct);
                        if(!string.IsNullOrWhiteSpace(hash))
                            found[algo] = hash;
                    }
                    catch(Exception ex)
                    {
                        _logger.LogWarning(ex, "IDestinationInspector failed for algorithm {Algo} on {DestPath}", algo, destPath);
                    }
                }
            }

            if(found.Count > 0)
            {
                item.Metadata.Set(MetadataKey.DestinationHashes, found);
            }
        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex, "DestinationInspector failed for {SourcePath}", item.SourcePath);
        }

        return new object();
    }

    // SidecarReader utility is used instead of embedding sidecar parsing logic here.
}
