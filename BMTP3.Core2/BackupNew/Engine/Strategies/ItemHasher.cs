using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

public class ItemHasher : IItemHasher
{
	private readonly ILogger<ItemHasher> _logger;
	private readonly IHashGenerator _hashGenerator;

	public ItemHasher(ILogger<ItemHasher> logger, IHashGenerator hashGenerator)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_hashGenerator = hashGenerator ?? throw new ArgumentNullException(nameof(hashGenerator));
	}

	public async Task<Dictionary<HashType, string>> ComputeHashesAsync(
        IBackupItem item, 
        List<HashType> hashTypes, 
        IProgress<ulong> progress,
        CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(item);
		ArgumentNullException.ThrowIfNull(item.Content);

		var requested = (hashTypes ?? Enumerable.Empty<HashType>()).Distinct().ToList();
		if (requested.Count == 0)
			requested.Add(HashType.SHA2_256); // Default to SHA2_256 if none specified

		_logger.LogTrace("Computing hashes for item {itemId} from {itemPath} with types: {hashTypes}", item.Id, item.Metadata.Get<string>(MetadataKey.SourceFullPath) ?? "unknown", string.Join(", ", requested));

		try
		{
			await using var stream = await item.Content.OpenReadStreamAsync(ct);
            // Pass progress to the generator
			var hashes = (await _hashGenerator.ComputeHashesAsync(stream, requested, progress, ct)).ToDictionary(x => x.Key, x => x.Value);

			_logger.LogDebug("Hashes computed for item {itemId}", item.Id);
			return hashes;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to compute hashes for item {itemId} from {itemPath}", item.Id, item.Metadata.Get<string>(MetadataKey.SourceFullPath) ?? "unknown");
			throw;
		}
	}
}
