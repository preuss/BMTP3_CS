using BMTP3.Core2.NetBackupFlow.Extensions;
using System.Collections.Concurrent;

namespace BMTP3.Core2.BackupNew.Domain.Item;
/// <summary>
/// Flexible thread-safe property bag for all metadata associated with a backup item.
/// Designed to be enriched by each stage in the pipeline.
/// </summary>
public class BackupMetadata
{
	private readonly ConcurrentDictionary<MetadataKey, object?> _data = new();

	public DateTime? AuthoredDateTime
	{
		get { return Get<DateTime?>(MetadataKey.AuthoredDateTime); }
		set { Set(MetadataKey.AuthoredDateTime, value); }
	}

	public DateTime? CreatedDateTime
	{
		get { return Get<DateTime?>(MetadataKey.CreatedDateTime); }
		set { Set(MetadataKey.CreatedDateTime, value); }
	}

	public DateTime? ModifiedDateTime
	{
		get { return Get<DateTime?>(MetadataKey.ModifiedDateTime); }
		set { Set(MetadataKey.ModifiedDateTime, value); }
	}

	public DateTime? AccessedDateTime
	{
		get { return Get<DateTime?>(MetadataKey.AccessedDateTime); }
		set { Set(MetadataKey.AccessedDateTime, value); }
	}

	public DateTime? MetadataChangedDatetime
	{
		get { return Get<DateTime?>(MetadataKey.MetadataChangedDateTime); }
		set { Set(MetadataKey.MetadataChangedDateTime, value); }
	}

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
	public T? Get<T>(MetadataKey key, bool useDefault = false, T? defaultValue = default)
	{
		if(!_data.TryGetValue(key, out var value))
		{
			return default;
		}
		if(value is null)
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
		} catch(Exception ex) when(ex is InvalidCastException or FormatException or OverflowException)
		{
			if(useDefault)
			{
				// Conversion failed, return default rather than crash
				// In a stricter system we might throw, but for metadata retrieval best-effort is often preferred.
				return defaultValue;
			}
			throw new InvalidOperationException($"Cannot convert metadata value for key '{key.ToKeyString()}' to type {typeof(T).Name}. Stored type: {value.GetType().Name}, Value: {value}", ex);
		}
	}

	/// <summary>
	/// Gets a required value – throws clear exception if missing.
	/// </summary>
	public T GetRequired<T>(MetadataKey key)
	{
		var value = Get<T>(key);
		if(value is null)
		{
			throw new InvalidOperationException($"Required metadata key '{key.ToKeyString()}' is missing.");
		}

		return value;
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

	/// <summary>
	/// Returns all current keys (for debugging or serialization).
	/// </summary>
	public ICollection<MetadataKey> Keys => _data.Keys;

	/// <summary>
	/// Converts metadata to a serializable dictionary using readable string keys.
	/// </summary>
	public IDictionary<string, object?> ToDictionary() => _data.ToDictionary(kvp => kvp.Key.ToKeyString(), kvp => kvp.Value);
}