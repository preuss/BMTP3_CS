using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Reader;
/// <summary>
/// Defines a generic contract for scanning a source and producing raw file entries.
/// This interface does not transform entries into backup items – it only enumerates them.
/// </summary>
public interface IFileSourceScanner<TFileInfo>
{
	/// <summary>
	/// Recursively scans the source and returns all file entries.
	/// </summary>
	IEnumerable<TFileInfo> ScanAll();
}