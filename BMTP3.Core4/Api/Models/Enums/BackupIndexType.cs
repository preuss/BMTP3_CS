namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Defines the format for the centralized backup index / catalog file.
/// The index provides an overview of all files in the backup.
/// </summary>
public enum BackupIndexType
{
    /// <summary>
    /// No central index file is generated.
    /// </summary>
    None,

    /// <summary>
    /// A single JSON catalog file (e.g. backup_catalog.json).
    /// </summary>
    Json,

    /// <summary>
    /// A SQLite database catalog (e.g. backup.db).
    /// </summary>
    Database
}
