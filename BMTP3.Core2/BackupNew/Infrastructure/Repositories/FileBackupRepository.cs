using System.Text.Json;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Infrastructure.Repositories;

// Simple file-based repository that stores BackupSessionEntity as JSON in a specified folder.
public class FileBackupRepository : IBackupRepository
{
	private readonly ILogger<FileBackupRepository>? _logger;
	private readonly string _storePath;
	private BackupSessionEntity? _currentSession;
	private List<BackupResumeRecord> _records = new();

	public FileBackupRepository(string? storePath = null, ILogger<FileBackupRepository>? logger = null)
	{
		_logger = logger;
		_storePath = string.IsNullOrWhiteSpace(storePath)
			? Path.Combine(Path.GetTempPath(), "bmtp3_sessions")
			: storePath!;
	}

	public Task<BackupSessionEntity?> LoadAsync(CancellationToken ct)
	{
		string path = Path.Combine(_storePath, "last_session.json");
		if (!File.Exists(path))
		{
			return Task.FromResult<BackupSessionEntity?>(null);
		}

		string text = File.ReadAllText(path);
		JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
		BackupSessionEntity? session = JsonSerializer.Deserialize<BackupSessionEntity>(text, options);
		return Task.FromResult(session);
	}

	public Task SaveAsync(BackupSessionEntity session, CancellationToken ct)
	{
		_currentSession = session;
		_records = session.Records ?? new List<BackupResumeRecord>();

        string path = Path.Combine(_storePath, "last_session.json");
		// Ensure storage directory exists lazily when actually saving session data
		if (!Directory.Exists(_storePath))
		{
			Directory.CreateDirectory(_storePath);
		}
		JsonSerializerOptions options = new() { WriteIndented = true };
		string text = JsonSerializer.Serialize(session, options);

		// Write using a temp-file + replace strategy with retries to avoid transient file-lock collisions
		int attempts = 5;
		for (int attempt = 1; attempt <= attempts; attempt++)
		{
			string tempPath = path + "." + Guid.NewGuid().ToString("n") + ".tmp";
			try
			{
				File.WriteAllText(tempPath, text);
				// Overwrite target
				File.Copy(tempPath, path, true);
				File.Delete(tempPath);
				_logger?.LogDebug("Saved backup session to {Path}", path);
				return Task.CompletedTask;
			}
			catch (IOException) when (attempt < attempts)
			{
				// Transient file lock - wait and retry
				try
				{
					Task.Delay(100 * attempt, ct).GetAwaiter().GetResult();
				}
				catch (OperationCanceledException)
				{
					// Propagate cancellation
					throw;
				}
				finally
				{
					// Best-effort cleanup of temp file
					try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
				}
			}
			catch (Exception ex)
			{
				// If it's the last attempt or non-IO error, surface it
				try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
				_logger?.LogWarning(ex, "Failed to save backup session to {Path}", path);
				throw;
			}
		}
		// If we reach here something went wrong – throw generic IO exception
		throw new IOException($"Failed to write backup session to '{path}' after {attempts} attempts.");
	}

	public Task PersistItemStateAsync(IBackupItem item, CancellationToken ct)
	{
		if (_currentSession == null)
		{
			_logger?.LogWarning("No session to persist item state. Call SaveAsync first.");
			return Task.CompletedTask;
		}

		// Extract relevant metadata for resume record
		string sourceId = item.Metadata.Get<string>(MetadataKey.SourceId) ?? "";
		string sourcePath = item.Metadata.Get<string>(MetadataKey.SourceFullPath) ?? "";
		string sourceFileName = item.Metadata.Get<string>(MetadataKey.SourceFileName) ?? "unknown";
		ulong length = item.Metadata.Get<ulong>(MetadataKey.Length);

		DateTimeOffset? dateCreated = item.Metadata.Get<DateTimeOffset?>(MetadataKey.CreatedDateTime);
		DateTimeOffset? dateModified = item.Metadata.Get<DateTimeOffset?>(MetadataKey.ModifiedDateTime);
		DateTimeOffset? dateAuthored = item.Metadata.Get<DateTimeOffset?>(MetadataKey.AuthoredDateTime);

		PersistState state = item.ResultState switch
		{
			ItemResultState.Success => PersistState.Completed,
			ItemResultState.Skipped => PersistState.Skipped,
			_ => PersistState.Pending
		};

		// Check if record already exists and update, or add new
		BackupResumeRecord? existing = _records.FirstOrDefault(r => r.ItemId == item.Id);
		if (existing != null)
		{
			existing.State = state;
			existing.BackupDate = state == PersistState.Completed ? DateTimeOffset.UtcNow : null;
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
				BackupDate = state == PersistState.Completed ? DateTimeOffset.UtcNow : null
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
			if (Directory.Exists(_storePath))
			{
				Directory.Delete(_storePath, true);
				Directory.CreateDirectory(_storePath);
			}
		}
		catch
		{
			// Swallow exceptions for best-effort deletion in cleanup scenarios.
			// Higher-level callers can still rely on file-system errors being surfaced elsewhere.
		}

		return Task.CompletedTask;
	}
}