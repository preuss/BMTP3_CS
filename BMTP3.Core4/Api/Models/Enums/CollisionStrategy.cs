namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Defines how name collisions at the destination are handled.
/// </summary>
public enum CollisionStrategy
{
	/// <summary>
	/// Skip the file if a destination file already exists.
	/// </summary>
	Skip,

	/// <summary>
	/// Overwrite the existing destination file.
	/// </summary>
	Overwrite,

	/// <summary>
	/// Generate a new unique name for the destination file.
	/// </summary>
	Rename
}