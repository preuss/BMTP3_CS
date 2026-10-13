using BMTP3.Core4.Engine.Exceptions;
using System.Text.Json;

namespace BMTP3.Core4.State;

internal sealed class BackupJsonSummaryStore : ISummaryStore
{
	private readonly string _storeDirectory;
	private readonly string _storeFilePrefix;

	public BackupJsonSummaryStore(string storeDirectory, string sessionId)
	{
		ArgumentNullException.ThrowIfNull(storeDirectory);
		ArgumentNullException.ThrowIfNull(sessionId);
		_storeDirectory = storeDirectory;
		_storeFilePrefix = GetStoreFilePrefix(sessionId);
	}

	private static string GetStoreFilePrefix(string sessionId) => $"session_{sessionId[..12]}";

	private string FilePath => Path.Combine(_storeDirectory, $"{_storeFilePrefix}.json");

	public FileInfo? StoreFile => new FileInfo(FilePath);

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
	};

	public async Task SaveAsync(BackupSummary summary, CancellationToken cancellationToken)
	{
		// The store owns the file format: always stamp the current session version,
		// no matter who constructed the summary.
		summary = summary with { SessionVersion = BackupSummary.CurrentVersion };

		string json = JsonSerializer.Serialize(summary, JsonOptions);
		string tempPath = FilePath + ".tmp";
		await File.WriteAllTextAsync(tempPath, json, cancellationToken);
		File.Move(tempPath, FilePath, overwrite: true);
	}


	public async Task<BackupSummary?> LoadAsync(CancellationToken cancellationToken)
	{
		if (!File.Exists(FilePath))
		{
			return null;
		}

		BackupSummary summary;

		try
		{
			await using FileStream stream = File.OpenRead(FilePath);
			summary = await JsonSerializer.DeserializeAsync<BackupSummary>(stream, cancellationToken: cancellationToken)
				   ?? throw new InvalidDataException($"Backup summary file is empty or invalid: {FilePath}");
		} catch (JsonException ex)
		{
			throw new InvalidDataException($"Backup summary file contains invalid JSON: {FilePath}", ex);
		} catch (IOException ex)
		{
			throw new IOException($"Failed to read backup summary file: {FilePath}", ex);
		} catch (UnauthorizedAccessException ex)
		{
			throw new UnauthorizedAccessException($"Access denied while reading backup summary file: {FilePath}", ex);
		}

		// Any version other than the current one makes the file unusable — treated exactly
		// like unreadable session data: the caller fails, no resume behavior can rescue it.
		// A missing version means the file predates versioning (version 1).
		int foundVersion = summary.SessionVersion ?? 1;
		if(foundVersion != BackupSummary.CurrentVersion)
		{
			throw new SessionVersionMismatchException(foundVersion, BackupSummary.CurrentVersion, StoreFile);
		}

		return summary;
	}


	public Task DeleteAsync()
	{
		if (File.Exists(FilePath))
			File.Delete(FilePath);

		return Task.CompletedTask;
	}
}
