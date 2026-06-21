using BMTP3.Core4.Api;

namespace BMTP3.Consoles.Tests;

internal sealed class StubFileSystemPathResolver : IFileSystemPathResolver
{
	public string ResolveExistingPathDisplayCasing(string path) => Path.GetFullPath(path);
}
