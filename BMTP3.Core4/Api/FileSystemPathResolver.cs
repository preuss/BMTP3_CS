using BMTP3.Core4.Helpers;

namespace BMTP3.Core4.Api;

public sealed class FileSystemPathResolver : IFileSystemPathResolver
{
	public string ResolveExistingPathDisplayCasing(string path)
	{
		return PathDisplayCasing.ResolveExistingPathDisplayCasing(path);
	}
}
