using BMTP3.Core2.BackupNew.Domain.Item;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
/// Skeleton implementation of a JSON sidecar generator.
/// Currently just a placeholder.
/// </summary>
public class JsonSidecarGenerator : ISidecarGenerator
{
	private readonly ILogger<JsonSidecarGenerator> _logger;

	public JsonSidecarGenerator(ILogger<JsonSidecarGenerator> logger)
	{
		_logger = logger;
	}

	public Task<bool> GenerateAsync(IBackupItem item, CancellationToken ct)
	{
		// TODO: Implement actual JSON serialization of item.Metadata
		// string json = JsonSerializer.Serialize(item.Metadata);
		// File.WriteAllText(path + ".json", json);

		_logger.LogDebug("Mocking sidecar generation for {SourceFileName}", item.Metadata.Get<string>(MetadataKey.SourceFileName));

		// Log to item audit trail
		item.AddLog("Sidecar generation skipped (Skeleton implementation)", "Sidecar");

		return Task.FromResult(true);
	}
}
