using System.Text.Json;

namespace BMTP3.Core4.State;

internal sealed class BackupJsonSummaryStore : ISummaryStore
{
	private readonly string _storeDirectory;
	private readonly string _sessionId;

	public BackupJsonSummaryStore(string storeDirectory, string sessionId)
	{
		ArgumentNullException.ThrowIfNull(storeDirectory);
		ArgumentNullException.ThrowIfNull(sessionId);
		_storeDirectory = storeDirectory;
		_sessionId = sessionId;
	}

	private string FilePath => Path.Combine(_storeDirectory, $"{_sessionId}.json");

	public FileInfo? StoreFile => new FileInfo(FilePath);

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
	};

	public async Task SaveAsync(BackupSummary summary, CancellationToken cancellationToken)
	{
		string json = JsonSerializer.Serialize(summary, JsonOptions);
		string tempPath = FilePath + ".tmp";
		await File.WriteAllTextAsync(tempPath, json, cancellationToken);
		File.Move(tempPath, FilePath, overwrite: true);
	}

	public Task<BackupSummary?> LoadAsync()
	{
		if(!File.Exists(FilePath))
			return Task.FromResult<BackupSummary?>(null);

		string json = File.ReadAllText(FilePath);
		BackupSummary? summary = JsonSerializer.Deserialize<BackupSummary>(json);
		return Task.FromResult(summary);
	}

	public Task DeleteAsync()
	{
		if(File.Exists(FilePath))
			File.Delete(FilePath);

		return Task.CompletedTask;
	}
}
