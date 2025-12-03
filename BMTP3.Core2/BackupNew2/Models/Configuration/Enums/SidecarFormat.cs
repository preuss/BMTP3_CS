namespace BMTP3.Core2.BackupNew2.Models.Configuration.Enums;

/// <summary>Defines the type of metadata file (sidecar) to be created next to each backed-up file.</summary>
public enum SidecarFormat
{
	None,       // No metadata file is created per file.
	Ini,        // Creates an INI file (e.g., file.jpg.ini) next to each file.
	Json,       // Creates a JSON file (e.g., file.jpg.json) next to each file.
}
