using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Strategies; // Added for IItemHasher
using Microsoft.Extensions.Logging; // Added for ILogger
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps.HashStep;

public class HashItemStep : IBackupItemStep<HashStepContext, HashStepResult>
{
	public string Name => "Hashing";
    public FilePhase Phase => FilePhase.Hashing; 

	private readonly HashStepContext _context;
	private readonly IItemHasher _itemHasher; // Injected IItemHasher
	private readonly ILogger<HashItemStep> _logger; // Injected ILogger

	public HashStepContext Context => _context;

	public HashItemStep(HashStepContext context, IItemHasher itemHasher, ILogger<HashItemStep> logger)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_itemHasher = itemHasher ?? throw new ArgumentNullException(nameof(itemHasher));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<HashStepResult> ExecuteAsync(IBackupItem item, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(item);
		ArgumentNullException.ThrowIfNull(item.Content);

		_logger.LogTrace("Executing HashItemStep for item {itemId} from {itemPath}", item.Id, item.SourcePath);

		try
		{
			// Check if hashes are already present and if recomputation is not forced
			var existingHashes = item.Metadata.Get<Dictionary<HashType, string>>(MetadataKey.Hashes) ?? new Dictionary<HashType, string>();
			var hashesToCompute = _context.HashTypes.Where(ht => _context.ForceRecompute || !existingHashes.ContainsKey(ht)).ToList();

			Dictionary<HashType, string> computedHashes = new();

			if (hashesToCompute.Any())
			{
				_logger.LogDebug("Computing {count} new hashes for item {itemId}", hashesToCompute.Count, item.Id);
				computedHashes = await _itemHasher.ComputeHashesAsync(item, hashesToCompute, ct);
			} else {
				_logger.LogDebug("No new hashes to compute for item {itemId}. Reusing existing.", item.Id);
			}

			// Merge computed hashes with existing ones
			foreach (var entry in computedHashes)
			{
				existingHashes[entry.Key] = entry.Value;
			}

			// Store updated hashes in metadata
			item.Metadata.Set(MetadataKey.Hashes, existingHashes);

			_logger.LogDebug("HashItemStep completed for item {itemId}", item.Id);
			return new HashStepResult { Hashes = existingHashes };
		}
		catch (Exception ex)
		{
			item.Fail($"Hash calculation failed: {ex.Message}", Name, ex);
			_logger.LogError(ex, "HashItemStep failed for item {itemId} from {itemPath}", item.Id, item.SourcePath);
			throw; // Re-throw to propagate error in the pipeline
		}
	}
}
