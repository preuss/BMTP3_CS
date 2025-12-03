namespace BMTP3.Consoles.ParserElements;
/// <summary>Defines the strategy for renaming a file when CollisionResolutionTypes is 'Rename'.</summary>
public enum RenameStrategies
{
	Increment,                 // Appends a sequential number (e.g., file_1.ext).
	Timestamp,                 // Appends a timestamp to the file name.
	Hash,                      // Appends a short content hash to the file name.
	CustomCollisionFilePath    // Uses a custom pattern defined in CustomCollisionOutputFilePath.
}