using BMTP3.Core4.Engine.Compare;

namespace BMTP3.Core4.Tests.Fakes;

internal sealed class FakeFileCompareService : IFileCompareService
{
	private readonly Func<string, string, CancellationToken, Task<bool>> _handler;

	public FakeFileCompareService() : this((_, _, _) => Task.FromResult(false))
	{
	}

	public FakeFileCompareService(Func<string, string, CancellationToken, Task<bool>> handler)
	{
		_handler = handler;
	}

	public Task<bool> CompareAsync(string sourcePath, string targetPath, CancellationToken ct) =>
		_handler(sourcePath, targetPath, ct);
}
