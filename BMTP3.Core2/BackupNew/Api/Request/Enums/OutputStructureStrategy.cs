namespace BMTP3.Core2.BackupNew.Api.Request.Enums;

/// <summary>Defines the strategy for creating the destination folder structure.</summary>
public enum OutputStructureStrategy
{
	PreserveSourceTree, // Preserves the original folder hierarchy from the source.
	Flat,               // Places all files directly into the root destination folder.
	CustomPathPattern,  // Uses the PathPattern to define the entire path, including folders and filename.
}
