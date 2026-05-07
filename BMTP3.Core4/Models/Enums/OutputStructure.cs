namespace BMTP3.Core4.Models.Enums;

/// <summary>
/// Defines how files are laid out in the destination directory.
/// </summary>
public enum OutputStructure
{
	/// <summary>
	/// All files are placed in a single directory.
	/// Subdirectory structure from the source is not preserved.
	/// </summary>
	Flat,

	/// <summary>
	/// The source directory structure is preserved in the destination.
	/// </summary>
	PreserveHierarchy
}