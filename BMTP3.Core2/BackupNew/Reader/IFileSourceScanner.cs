using System.Collections.Generic;

namespace BMTP3.Core2.BackupNew.Reader;

/// <summary>
/// Defines a generic contract for scanning a source and producing raw file entries.
/// This interface does not transform entries into backup items – it only enumerates them.
/// </summary>
public interface IFileSourceScanner<TFileInfo>
{
	/// <summary>
	/// Traverses the source and returns all file entries.
	/// </summary>
	IEnumerable<TFileInfo> TraverseFiles(bool recursive = true, Events.TraversalProgressCounter? progress = null);
}