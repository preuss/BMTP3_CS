namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Defines the strategy for how files are laid out in the destination directory.
/// </summary>
public enum OutputStructureStrategy
{
	/// <summary>
	/// The source directory structure is preserved in the destination.
	/// </summary>
	PreserveHierarchy,

	/// <summary>
	/// All files are placed in a single directory.
	/// Subdirectory structure from the source is not preserved.
	/// </summary>
	Flat,

	/// <summary>
	/// A user-defined template pattern is used for the output path.
	/// Including both folder structure and filename.
	/// </summary>
	CustomPathPattern
}
