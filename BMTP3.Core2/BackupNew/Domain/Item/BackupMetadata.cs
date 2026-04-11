using System.Collections.Concurrent;
using BMTP3.Core2.BackupNew.Extensions;

namespace BMTP3.Core2.BackupNew.Domain.Item;

/// <summary>
///     Flexible thread-safe property bag for all metadata associated with a backup item.
///     Designed to be enriched by each stage in the pipeline.
/// </summary>
public class BackupMetadata
{
	private readonly ConcurrentDictionary<MetadataKey, object?> _data = new();

	public DateTimeOffset? AuthoredDateTime
	{
		get => Get<DateTimeOffset?>(MetadataKey.AuthoredDateTime);
		set => Set(MetadataKey.AuthoredDateTime, value);
	}

	public DateTimeOffset? CreatedDateTime
	{
		get => Get<DateTimeOffset?>(MetadataKey.CreatedDateTime);
		set => Set(MetadataKey.CreatedDateTime, value);
	}

	public DateTimeOffset? ModifiedDateTime
	{
		get => Get<DateTimeOffset?>(MetadataKey.ModifiedDateTime);
		set => Set(MetadataKey.ModifiedDateTime, value);
	}

	public DateTimeOffset? AccessedDateTime
	{
		get => Get<DateTimeOffset?>(MetadataKey.AccessedDateTime);
		set => Set(MetadataKey.AccessedDateTime, value);
	}

	public DateTimeOffset? MetadataChangedDatetime
	{
		get => Get<DateTimeOffset?>(MetadataKey.MetadataChangedDateTime);
		set => Set(MetadataKey.MetadataChangedDateTime, value);
	}

	/// <summary>
	///     Returns all current keys (for debugging or serialization).
	/// </summary>
	public ICollection<MetadataKey> Keys => _data.Keys;

	/// <summary>
	///     Sets a value. Overwrites existing key. Removes the key if value is null.
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
	///     Gets a value, or null if the key does not exist.
	///     Supports transparent coercion between <see cref="DateTime" /> and <see cref="DateTimeOffset" />:
	///     a stored <see cref="DateTime" /> can be retrieved as <see cref="DateTimeOffset" /> and vice-versa.
	/// </summary>
	public T? Get<T>(MetadataKey key, bool useDefault = false, T? defaultValue = default)
	{
		if(!_data.TryGetValue(key, out object? value))
		{
			return default;
		}

		if(value is null)
		{
			return default;
		}

		// Fast path: stored type matches requested type exactly.
		if(value is T t)
		{
			return t;
		}

		// Transparent coercion: DateTime ↔ DateTimeOffset.
		// Convert.ChangeType does not support these types, so we handle them explicitly.
		Type target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

		if(target == typeof(DateTimeOffset) && value is DateTime dt)
		{
			// DateTime → DateTimeOffset: preserve the wall-clock value.
			// Unspecified kind is treated as UTC to avoid silent Local→UTC shifts.
			DateTimeOffset dto = dt.Kind == DateTimeKind.Unspecified
				? new DateTimeOffset(dt, TimeSpan.Zero)
				: new DateTimeOffset(dt);
			return (T)(object)dto;
		}

		if(target == typeof(DateTime) && value is DateTimeOffset dto2)
		{
			// DateTimeOffset → DateTime: use UTC representation for predictable behaviour.
			return (T)(object)dto2.UtcDateTime;
		}

		try
		{
			return (T)Convert.ChangeType(value, typeof(T));
		} catch(Exception ex) when(ex is InvalidCastException or FormatException or OverflowException)
		{
			if(useDefault)
			{
				return defaultValue;
			}

			throw new InvalidOperationException(
				$"Cannot convert metadata value for key '{key.ToKeyString()}' to type {typeof(T).Name}. Stored type: {value.GetType().Name}, Value: {value}",
				ex);
		}
	}

	/// <summary>
	///     Gets a required value – throws clear exception if missing.
	/// </summary>
	public T GetRequired<T>(MetadataKey key)
	{
		T? value = Get<T>(key);
		if(value is null)
		{
			throw new InvalidOperationException($"Required metadata key '{key.ToKeyString()}' is missing.");
		}

		return value;
	}

	/// <summary>
	///     Checks if a key exists.
	/// </summary>
	public bool Has(MetadataKey key)
	{
		return _data.ContainsKey(key);
	}

	/// <summary>
	///     Returns all stored metadata as a read-only dictionary.
	/// </summary>
	public IReadOnlyDictionary<MetadataKey, object?> GetAll()
	{
		return _data.ToDictionary(k => k.Key, v => v.Value);
	}

	/// <summary>
	///     Converts metadata to a serializable dictionary using readable string keys.
	/// </summary>
	public IDictionary<string, object?> ToDictionary()
	{
		return _data.ToDictionary(kvp => kvp.Key.ToKeyString(), kvp => kvp.Value);
	}
}