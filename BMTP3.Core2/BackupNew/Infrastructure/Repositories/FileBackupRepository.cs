using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Domain.Repositories;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using BMTP3.Core2.BackupNew.Api.Enums;

namespace BMTP3.Core2.BackupNew.Infrastructure.Repositories;

// Simple file-based repository that stores BackupSessionEntity as JSON in a specified folder.
public class FileBackupRepository : IBackupRepository
{
    private readonly string _storePath;
    private readonly Microsoft.Extensions.Logging.ILogger<FileBackupRepository>? _logger;
    private List<BackupResumeRecord> _records = new();
    private BackupSessionEntity? _currentSession;

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
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		BackupSessionEntity? session = JsonSerializer.Deserialize<BackupSessionEntity>(text, options);
		return Task.FromResult(session);
	}

    public Task SaveAsync(BackupSessionEntity session, CancellationToken ct)
    {
        _currentSession = session;
        _records = session.Records ?? new List<BackupResumeRecord>();
        
        string path = Path.Combine(_storePath, "last_session.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        string text = JsonSerializer.Serialize(session, options);
        File.WriteAllText(path, text);
        _logger?.LogDebug("Saved backup session to {Path}", path);
        return Task.CompletedTask;
    }

    public Task PersistItemStateAsync(IBackupItem item, CancellationToken ct)
    {
        if(_currentSession == null)
        {
            _logger?.LogWarning("No session to persist item state. Call SaveAsync first.");
            return Task.CompletedTask;
        }

        // Extract relevant metadata for resume record
        string sourceId = item.Metadata.Get<string>(MetadataKey.SourceId) ?? "";
        string sourcePath = item.Metadata.Get<string>(MetadataKey.SourceFullPath) ?? "";
        string sourceFileName = item.Metadata.Get<string>(MetadataKey.SourceFileName) ?? "unknown";
        ulong length = item.Metadata.Get<ulong>(MetadataKey.Length);
        
        DateTime? dateCreated = item.Metadata.Has(MetadataKey.CreatedDateTime) 
            ? item.Metadata.Get<DateTime>(MetadataKey.CreatedDateTime) 
            : null;
        DateTime? dateModified = item.Metadata.Has(MetadataKey.ModifiedDateTime) 
            ? item.Metadata.Get<DateTime>(MetadataKey.ModifiedDateTime) 
            : null;
        DateTime? dateAuthored = item.Metadata.Has(MetadataKey.AuthoredDateTime) 
            ? item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime) 
            : null;

        PersistState state = item.ResultState switch
        {
            ItemResultState.Success => PersistState.Completed,
            ItemResultState.Skipped => PersistState.Skipped,
            _ => PersistState.Pending
        };

        // Check if record already exists and update, or add new
        var existing = _records.FirstOrDefault(r => r.ItemId == item.Id);
        if(existing != null)
        {
            existing.State = state;
            existing.BackupDate = state == PersistState.Completed ? DateTime.UtcNow : null;
        }
        else
        {
            _records.Add(new BackupResumeRecord
            {
                ItemId = item.Id,
                SourceId = sourceId,
                SourcePath = sourcePath,
                SourceFileName = sourceFileName,
                LengthBytes = length,
                DateCreated = dateCreated,
                DateModified = dateModified,
                DateAuthored = dateAuthored,
                State = state,
                BackupDate = state == PersistState.Completed ? DateTime.UtcNow : null
            });
        }

        // Update session with current records
        _currentSession.Records = _records;

        _logger?.LogDebug("Persisted item {ItemId}: {State}", item.Id, state);
        return Task.CompletedTask;
    }

	public Task DeleteAsync(CancellationToken ct = default)
	{
		try
		{
			if(Directory.Exists(_storePath))
			{
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
