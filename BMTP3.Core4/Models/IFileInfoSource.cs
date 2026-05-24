namespace BMTP3.Core4.Models;

internal interface IFileInfoSource
{
	bool TryGetFileInfo(out FileInfo fileInfo);
}
