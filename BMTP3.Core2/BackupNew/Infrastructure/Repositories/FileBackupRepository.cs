using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Domain.Item;
using System.IO;

namespace BMTP3.Core2.BackupNew.Infrastructure.Repositories;

// Simple file-based repository that stores BackupSessionEntity as JSON in a specified folder.
public class FileBackupRepository : IBackupRepository
{
	private readonly string _storePath;

	public FileBackupRepository(string? storePath = null)
	{
		_storePath = string.IsNullOrWhiteSpace(storePath) ? Path.Combine(Path.GetTempPath(), "bmtp3_sessions") : storePath!;
		Directory.CreateDirectory(_storePath);
	}

	public Task<BackupSessionEntity?> LoadAsync(CancellationToken ct)
	{
		string path = Path.Combine(_storePath, "last_session.json");
		if(!File.Exists(path)) return Task.FromResult<BackupSessionEntity?>(null);
		string text = File.ReadAllText(path);
		var session = JsonSerializer.Deserialize<BackupSessionEntity>(text);
		return Task.FromResult(session);
	}

	public Task SaveAsync(BackupSessionEntity session, CancellationToken ct)
	{
		string path = Path.Combine(_storePath, "last_session.json");
		string text = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(path, text);
		return Task.CompletedTask;
	}

	public Task PersistItemStateAsync(BackupItem item, CancellationToken ct)
	{
		// Simple append-log of failing items for diagnostics (not full implementation)
		string logPath = Path.Combine(_storePath, "item_states.log");
		string line = $"{DateTime.UtcNow:o} - Item {item.Metadata.Get<string>(MetadataKey.SourceFileName) ?? "?"} - State {item.ResultState}\n";
		File.AppendAllText(logPath, line);
		return Task.CompletedTask;
	}

	public Task DeleteAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}
}