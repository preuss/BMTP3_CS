using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Domain.Repositories;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Infrastructure.Repositories;

// Simple file-based repository that stores BackupSessionEntity as JSON in a specified folder.
public class FileBackupRepository : IBackupRepository
{
    private readonly string _storePath;
    private readonly Microsoft.Extensions.Logging.ILogger<FileBackupRepository>? _logger;

    public FileBackupRepository(string? storePath = null, Microsoft.Extensions.Logging.ILogger<FileBackupRepository>? logger = null)
    {
        _logger = logger;
        _storePath = string.IsNullOrWhiteSpace(storePath) ? Path.Combine(Path.GetTempPath(), "bmtp3_sessions") : storePath!;
        Directory.CreateDirectory(_storePath);
    }

	public Task<BackupSessionEntity?> LoadAsync(CancellationToken ct)
	{
		string path = Path.Combine(_storePath, "last_session.json");
		if(!File.Exists(path)) return Task.FromResult<BackupSessionEntity?>(null);
		string text = File.ReadAllText(path);
		BackupSessionEntity? session = JsonSerializer.Deserialize<BackupSessionEntity>(text);
		return Task.FromResult(session);
	}

    public Task SaveAsync(BackupSessionEntity session, CancellationToken ct)
    {
        string path = Path.Combine(_storePath, "last_session.json");
        string text = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
        // Persist the session to disk. This is the intended repository behavior; log via ILogger for traceability.
        File.WriteAllText(path, text);
        _logger?.LogDebug("Saved backup session to {Path}", path);
        return Task.CompletedTask;
    }

    public Task PersistItemStateAsync(BackupItem item, CancellationToken ct)
    {
        // Previously this method appended a plain-text line to item_states.log inside the repo folder.
        // That produced side-effect files under the repository during tests. Replace with structured logging.
        string src = item.Metadata.Get<string>(MetadataKey.SourceFileName) ?? "?";
        _logger?.LogInformation("PersistItemState: Item {SourceFileName} State {State}", src, item.ResultState);
        return Task.CompletedTask;
    }

	public Task DeleteAsync(CancellationToken ct = default)
	{
		try
		{
			if(Directory.Exists(_storePath))
			{
				// Delete the directory and recreate to keep repository in a clean state
				Directory.Delete(_storePath, recursive: true);
				Directory.CreateDirectory(_storePath);
			}
		} catch
		{
			// Swallow exceptions for best-effort deletion in cleanup scenarios.
			// Higher-level callers can still rely on file-system errors being surfaced elsewhere.
		}
		return Task.CompletedTask;
	}
}
