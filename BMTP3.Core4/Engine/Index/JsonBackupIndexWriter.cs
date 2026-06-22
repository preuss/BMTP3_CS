using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BMTP3.Core4.Engine.Index;

internal sealed class JsonBackupIndexWriter : IBackupIndexWriter
{
	private readonly ILogger<JsonBackupIndexWriter> _logger;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
	};

	public JsonBackupIndexWriter(ILogger<JsonBackupIndexWriter> logger)
	{
		_logger = logger;
	}

	public async Task WriteAsync(
		string destinationDirectory,
		string sessionId,
		IReadOnlyList<BackupRecord> records,
		BackupPlan plan,
		BackupResult result,
		CancellationToken cancellationToken
	)
	{
		string catalogDir = Path.Combine(destinationDirectory, ".bmtp3", sessionId);
		Directory.CreateDirectory(catalogDir);

		BackupIndexCatalog catalog = BuildCatalog(records, sessionId, plan, result);

		string json = JsonSerializer.Serialize(catalog, JsonOptions);

		string filePath = Path.Combine(catalogDir, "backup_catalog.json");
		string tempPath = filePath + ".tmp";

		await File.WriteAllTextAsync(tempPath, json, cancellationToken);
		File.Move(tempPath, filePath, overwrite: true);

		_logger.LogInformation("Backup catalog written: {Path}", filePath);
	}

	private static BackupIndexCatalog BuildCatalog(
		IReadOnlyList<BackupRecord> records,
		string sessionId,
		BackupPlan plan,
		BackupResult result
	)
	{
		long totalBytes = records.Sum(r => (long)r.Item.Content.Length);
		int completedFiles = records.Count(r => r.Status == BackupItemStatus.Succeeded);

		List<BackupIndexFileEntry> files = new(records.Count);

		foreach (BackupRecord record in records)
		{
			BackupIndexFileEntry entry = new()
			{
				Id = record.Item.Id,
				SourcePath = record.Item.SourcePath,
				RelativePath = record.Item.RelativeFilePath ?? string.Empty,
				FileName = record.Item.FileName,
				DestinationPath = record.DestinationPath,
				Length = (long)record.Item.Content.Length,
				Status = MapStatus(record.Status),
				Hashes = MapHashes(record.Metadata.ComputedHashes),
				Timestamps = MapTimestamps(record.Metadata),
			};

			files.Add(entry);
		}

		return new BackupIndexCatalog
		{
			BackupName = plan.Name,
			CreatedAt = DateTimeOffset.UtcNow,
			SessionId = sessionId,
			SourcePath = plan.SourcePath,
			Destination = plan.Destination,
			TotalFiles = records.Count,
			TotalBytes = totalBytes,
			CompletedFiles = completedFiles,
			State = result.State.ToString(),
			Files = files.AsReadOnly(),
		};
	}

	private static Dictionary<string, string>? MapHashes(Dictionary<HashType, string>? computedHashes)
	{
		if (computedHashes == null || computedHashes.Count == 0)
			return null;

		Dictionary<string, string> result = new(computedHashes.Count);

		foreach (KeyValuePair<HashType, string> kvp in computedHashes)
		{
			result[kvp.Key.ToString()] = kvp.Value;
		}

		return result;
	}

	private static BackupIndexTimestamps MapTimestamps(ItemMetadata metadata)
	{
		return new BackupIndexTimestamps
		{
			MediaTaken = metadata.MediaTakenDateTime,
			Created = metadata.CreatedDateTime,
			Modified = metadata.ModifiedDateTime,
			Authored = metadata.AuthoredDateTime,
			Accessed = metadata.AccessedDateTime,
		};
	}

	private static string MapStatus(BackupItemStatus status) => status switch
	{
		BackupItemStatus.Succeeded => "Succeeded",
		BackupItemStatus.Failed => "Failed",
		BackupItemStatus.Skipped => "Skipped",
		BackupItemStatus.Pending => "Pending",
		_ => "Unknown",
	};
}
