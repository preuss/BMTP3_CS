namespace BMTP3.Core2.BackupNew.Models;

/// <summary>
/// A flexible container for file metadata, using strong-typed keys.
/// </summary>
public class Metadata
{
	private readonly Dictionary<MetadataKey, object> _data = new();

	/// <summary>
	/// Sets a metadata value.
	/// </summary>
	public void Set(MetadataKey key, object value)
	{
		_data[key] = value;
	}

	/// <summary>
	/// Retrieves a metadata value, cast to type T.
	/// Returns default(T) if key is missing or type mismatch (safely).
	/// </summary>
	public T? Get<T>(MetadataKey key)
	{
		if(_data.TryGetValue(key, out var value) && value is T typedValue)
		{
			return typedValue;
		}
		return default;
	}

	/// <summary>
	/// Checks if a specific key exists.
	/// </summary>
	public bool Has(MetadataKey key)
	{
		return _data.ContainsKey(key);
	}

	/// <summary>
	/// Returns all stored metadata as a read-only dictionary (useful for sidecar generation).
	/// </summary>
	public IReadOnlyDictionary<MetadataKey, object> GetAll()
	{
		return _data;
	}
}
