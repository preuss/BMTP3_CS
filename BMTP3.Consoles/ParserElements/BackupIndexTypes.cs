namespace BMTP3.Consoles.ParserElements;

/// <summary>Defines the centralized catalog or index structure for the entire backup set.</summary>
public enum BackupIndexTypes
{
	None, // No central metadata file or database is created.
	Json, // Creates a single JSON file containing all backup metadata (e.g., backup_catalog.json).
	Database // Creates a centralized SQLite database containing all metadata (e.g., backup.db).
}