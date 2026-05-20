namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Defines how files are compared to determine if a collision exists.
/// </summary>
public enum CollisionComparisonType
{
	/// <summary>
	/// No content comparison. A collision exists if the filename already exists.
	/// </summary>
	None,

	/// <summary>
	/// Compare file content using a hash algorithm.
	/// A collision exists only when the hash matches.
	/// </summary>
	Hash,

	/// <summary>
	/// Compare file content byte-by-byte.
	/// A collision exists only when all bytes match.
	/// </summary>
	Binary
}
