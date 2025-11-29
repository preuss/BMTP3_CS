using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Traversal;
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