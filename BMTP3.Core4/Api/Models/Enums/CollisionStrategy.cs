namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Defines how name collisions at the destination are handled.
/// </summary>
public enum CollisionStrategy
{
	/// <summary>
	/// Renames the new file using a specific strategy.
	/// </summary>
	Rename,

	/// <summary>
	/// Skip the file if a destination file already exists.
	/// </summary>
	Skip,

	/// <summary>
	/// Overwrite the existing destination file.
	/// </summary>
	Overwrite,

	/// <summary>
	/// Stops the backup process and treats the collision as an error.
	/// </summary>
	Error

}