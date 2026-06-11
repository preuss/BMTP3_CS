namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Defines how files are renamed when a collision is resolved by renaming.
/// </summary>
public enum RenameStrategy
{
	/// <summary>
	/// Append an incrementing number (e.g. photo.jpg → photo_1.jpg).
	/// </summary>
	Increment,

	/// <summary>
	/// Append a timestamp (e.g. photo.jpg → photo_20260517_143022.jpg).
	/// </summary>
	Timestamp,

	/// <summary>
	/// Append a short content hash (e.g. photo.jpg → photo_a3f2c8.jpg).
	/// </summary>
	Hash,

	/// <summary>
	/// Use a user-defined pattern.
	/// </summary>
	CustomPattern
}
