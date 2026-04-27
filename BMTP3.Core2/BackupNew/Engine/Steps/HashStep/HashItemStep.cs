using BMTP3.Core2.BackupNew.Api.Progress.Enums;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Steps.HashStep;

public class HashItemStep : IBackupItemStep<HashStepContext, HashStepResult>
{
	private readonly IItemHasher _itemHasher;
	private readonly ILogger<HashItemStep> _logger;

	public HashItemStep(HashStepContext context, IItemHasher itemHasher, ILogger<HashItemStep> logger)
	{
		Context = context ?? throw new ArgumentNullException(nameof(context));
		_itemHasher = itemHasher ?? throw new ArgumentNullException(nameof(itemHasher));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

   public string Name => "Hashing";
   public FilePhase Phase => FilePhase.Comparing; // 'Comparing' is the closest match in the new model

	public HashStepContext Context { get; }

	public async Task<HashStepResult> ExecuteAsync(IBackupItem item, IProgress<ulong> progress, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(item);
		ArgumentNullException.ThrowIfNull(item.Content);

		_logger.LogTrace("Executing HashItemStep for item {itemId} from {itemPath}", item.Id, item.SourcePath);

		try
		{
			Dictionary<HashType, string> existingHashes =
				item.Metadata.Get<Dictionary<HashType, string>>(MetadataKey.Hashes) ??
				new Dictionary<HashType, string>();
			List<HashType> hashesToCompute = Context.HashTypes
				.Where(ht => Context.ForceRecompute || !existingHashes.ContainsKey(ht)).ToList();

			Dictionary<HashType, string> computedHashes = new();

			if (hashesToCompute.Any())
			{
				_logger.LogDebug("Computing {count} new hashes for item {itemId}", hashesToCompute.Count, item.Id);

				// Pass progress reporter to the Hasher
				computedHashes = await _itemHasher.ComputeHashesAsync(item, hashesToCompute, progress, ct);
			}
			else
			{
				_logger.LogDebug("No new hashes to compute for item {itemId}. Reusing existing.", item.Id);
				// Report 100% (all bytes) if skipped? Or 0? Usually 0 if no work done.
				progress?.Report(item.Content.Length);
			}

			foreach (KeyValuePair<HashType, string> entry in computedHashes)
			{
				existingHashes[entry.Key] = entry.Value;
			}

			item.Metadata.Set(MetadataKey.Hashes, existingHashes);

			_logger.LogDebug("HashItemStep completed for item {itemId}", item.Id);
			return new HashStepResult { Hashes = existingHashes };
		}
		catch (Exception ex)
		{
			item.Fail($"Hash calculation failed: {ex.Message}", Name, ex);
			_logger.LogError(ex, "HashItemStep failed for item {itemId} from {itemPath}", item.Id, item.SourcePath);
			throw;
		}
	}
}