namespace BMTP3.Consoles.ParserElements;
/// <summary>Defines the type of resolution method to be applied when a file collision is detected.</summary>
public enum CollisionResolutionTypes
{
	Overwrite, // Overwrites the existing file.
	Skip,      // Skips the current file.
	Error,     // Stops the backup process.
	Rename     // Renames the new file using a specific strategy.
}