namespace BMTP3.Core2.BackupNew.Api.Request.Enums;

/// <summary>Defines the strategy for renaming a file when CollisionResolutionTypes is 'Rename'.</summary>
public enum RenameStrategy
{
	Increment, // Appends a sequential number (e.g., file_1.ext).
	Timestamp, // Appends a timestamp to the file name.
	Hash, // Appends a short content hash to the file name.
	CustomCollisionPathPattern // Uses a custom pattern defined in CollisionPathPattern.
}