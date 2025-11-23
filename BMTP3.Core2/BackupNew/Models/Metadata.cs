using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.BackupNew.Models;
/// <summary>
/// Flexible thread-safe property bag for all metadata associated with a backup item.
/// Designed to be enriched by each stage in the pipeline.
/// </summary>
public class Metadata {
	private readonly ConcurrentDictionary<MetadataKey, object?> _data = new();

	/// <summary>
	/// Sets a value. Overwrites existing key. Removes the key if value is null.
	/// </summary>
	public void Set(MetadataKey key, object? value) {
		if(value is null)
			_data.TryRemove(key, out _);
		else
			_data[key] = value;
	}

	/// <summary>
	/// Gets a value, or null if the key does not exist.
	/// </summary>
	public T? Get<T>(MetadataKey key) {
		if(!_data.TryGetValue(key, out var value))
			return default;

		if(value is T t)
			return t;

		try {
			return (T)Convert.ChangeType(value, typeof(T));
		} catch(Exception ex) when(ex is InvalidCastException or FormatException or OverflowException) {
			throw new InvalidOperationException(
				$"Cannot convert metadata value for key '{key.GetKey()}' to type {typeof(T).Name}. Stored type: {value.GetType().Name}",
				ex);
		}
	}

	/// <summary>
	/// Gets a required value – throws clear exception if missing.
	/// </summary>
	public T GetRequired<T>(MetadataKey key) {
		var value = Get<T>(key);
		if(value is null)
			throw new InvalidOperationException($"Required metadata key '{key.GetKey()}' is missing.");

		return value;
	}

	/// <summary>
	/// Checks if a key exists.
	/// </summary>
	public bool Has(MetadataKey key) => _data.ContainsKey(key);

	/// <summary>
	/// Returns all current keys (for debugging or serialization).
	/// </summary>
	public ICollection<MetadataKey> Keys => _data.Keys;

	/// <summary>
	/// Converts metadata to a serializable dictionary using readable string keys.
	/// </summary>
	public IDictionary<string, object?> ToDictionary() =>
		_data.ToDictionary(kvp => kvp.Key.GetKey(), kvp => kvp.Value);
}