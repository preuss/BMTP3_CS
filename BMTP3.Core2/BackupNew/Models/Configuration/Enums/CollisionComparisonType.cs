namespace BMTP3.Core2.BackupNew.Models.Configuration.Enums;

/// <summary>Defines how the existing file in the destination is compared to the source file before CollisionResolutionTypes is applied.</summary>
public enum CollisionComparisonType
{
	None,    // Skips content comparison. Proceeds directly to CollisionResolutionTypes (Overwrite, Rename, etc.).
	Hash,    // Compares file content using a hash algorithm (e.g., SHA-256). Skips copy if hashes are identical.
	Binary   // Compares file content byte-by-byte. Skips copy if files are bit-for-bit identical.
}
