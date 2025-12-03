namespace BMTP3.Core2.BackupNew2.Traversal;

public record TraversalProgress(
	// File metrics
	int FileCount,
	string? LastFileName,
	bool FileCountChanged,

	// Directory metrics
	int DirectoryCount,
	string? LastDirectoryName,
	bool DirectoryCountChanged
);
