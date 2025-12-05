using System.Text.Json;
using System.Text.Json.Serialization;

namespace BMTP3.Core2.BackupNew.Repositories;

/// <summary>
/// A simple JSON-based implementation of the backup state repository.
/// Stores a mapping of file hashes to their backup locations.
/// </summary>
public class JsonBackupRepository : IBackupRepository
{
	private readonly string _stateFilePath;
	private StateData _data = new();
	private readonly object _lock = new();
	private bool _isDirty = false;

	public JsonBackupRepository(string storageDirectory)
	{
		_stateFilePath = Path.Combine(storageDirectory, "backup_state.json");
	}

	public Task LoadAsync()
	{
		lock(_lock)
		{
			if(File.Exists(_stateFilePath))
			{
				try
				{
					string json = File.ReadAllText(_stateFilePath);
					var loadedData = JsonSerializer.Deserialize<StateData>(json);
					if(loadedData != null)
					{
						_data = loadedData;
					}
				} catch(Exception)
				{
					// In a real scenario, we might want to backup the corrupt file and start fresh,
					// or throw an error. For now, we start fresh but log nothing (as we don't have a logger injected yet).
					_data = new StateData();
				}
			} else
			{
				_data = new StateData();
			}
		}
		return Task.CompletedTask;
	}

	public Task SaveAsync()
	{
		lock(_lock)
		{
			if(!_isDirty) return Task.CompletedTask;

			_data.LastRun = DateTime.UtcNow;

			var options = new JsonSerializerOptions { WriteIndented = true };
			string json = JsonSerializer.Serialize(_data, options);

			// Ensure directory exists
			string? dir = Path.GetDirectoryName(_stateFilePath);
			if(!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			File.WriteAllText(_stateFilePath, json);
			_isDirty = false;
		}
		return Task.CompletedTask;
	}

	public bool IsHashKnown(string hash)
	{
		if(string.IsNullOrEmpty(hash)) return false;
		lock(_lock)
		{
			return _data.Items.ContainsKey(hash);
		}
	}

	public bool IsPathKnown(string relativeDestinationPath)
	{
		if(string.IsNullOrEmpty(relativeDestinationPath)) return false;

		// This is an O(N) operation in this simple structure. 
		// For millions of files, we would need a secondary index (Reverse Lookup).
		lock(_lock)
		{
			// Normalize slashes for comparison
			string target = relativeDestinationPath.Replace('\\', '/');

			foreach(var item in _data.Items.Values)
			{
				if(item.Paths.Contains(target)) return true;
			}
			return false;
		}
	}

	public void RegisterItem(string hash, string originalPath, string destinationPath, DateTime authoredDate)
	{
		lock(_lock)
		{
			if(!_data.Items.TryGetValue(hash, out var entry))
			{
				entry = new ItemEntry
				{
					FirstSeen = DateTime.UtcNow,
					AuthoredDate = authoredDate
				};
				_data.Items[hash] = entry;
			}

			// Normalize destination path
			string normDest = destinationPath.Replace('\\', '/');

			if(!entry.Paths.Contains(normDest))
			{
				entry.Paths.Add(normDest);
			}

			// Update metadata if needed (e.g. if we found a better date?)
			// For now, we stick to the first authored date found, or update if it was empty.
			if(entry.AuthoredDate == default)
			{
				entry.AuthoredDate = authoredDate;
			}

			_isDirty = true;
		}
	}

	// -------------------------------------------------------
	// Internal Data Models for JSON Serialization
	// -------------------------------------------------------

	private class StateData
	{
		[JsonPropertyName("lastRun")]
		public DateTime LastRun { get; set; }

		[JsonPropertyName("items")]
		public Dictionary<string, ItemEntry> Items { get; set; } = new();
	}

	private class ItemEntry
	{
		[JsonPropertyName("paths")]
		public List<string> Paths { get; set; } = new();

		[JsonPropertyName("dest")]
		public string FirstDestination => Paths.FirstOrDefault() ?? "";

		[JsonPropertyName("authored")]
		public DateTime AuthoredDate { get; set; }

		[JsonPropertyName("seen")]
		public DateTime FirstSeen { get; set; }
	}
}
