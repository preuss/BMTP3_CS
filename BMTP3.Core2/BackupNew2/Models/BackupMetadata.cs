using BMTP3.Core2.BackupNew2.Models.Configuration;
using System.Collections.Concurrent;

namespace BMTP3.Core2.BackupNew2.Models;

/// <summary>
/// Flexible thread-safe property bag for all metadata associated with a backup item.
/// Designed to be enriched by each stage in the pipeline.
/// </summary>
public class BackupMetadata
{
	private readonly ConcurrentDictionary<MetadataKey, object?> _data = new();

	/// <summary>
	/// Sets a value. Overwrites existing key. Removes the key if value is null.
	/// </summary>
	public void Set(MetadataKey key, object? value)
	{
		if(value is null)
		{
			_data.TryRemove(key, out _);
		} else
		{
			_data[key] = value;
		}
	}

	/// <summary>
	/// Gets a value, or null if the key does not exist.
	/// </summary>
	public T? Get<T>(MetadataKey key)
	{
		if(!_data.TryGetValue(key, out var value) || value is null)
		{
			return default;
		}

		if(value is T t)
		{
			return t;
		}

		try
		{
			return (T)Convert.ChangeType(value, typeof(T));
		} catch
		{
			// Conversion failed, return default rather than crash
			// In a stricter system we might throw, but for metadata retrieval best-effort is often preferred.
			return default;
		}
	}

	/// <summary>
	/// Checks if a key exists.
	/// </summary>
	public bool Has(MetadataKey key) => _data.ContainsKey(key);

	/// <summary>
	/// Returns all stored metadata as a read-only dictionary.
	/// </summary>
	public IReadOnlyDictionary<MetadataKey, object?> GetAll()
	{
		return _data.ToDictionary(k => k.Key, v => v.Value);
	}
}