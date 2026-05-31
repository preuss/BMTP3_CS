namespace BMTP3.Core4.Engine.Compare;

internal sealed class FileCompareService : IFileCompareService
{
	private readonly BinaryFileComparerSelector _selector;

	public FileCompareService(BinaryFileComparerSelector selector)
	{
		_selector = selector ?? throw new ArgumentNullException(nameof(selector));
	}

	public async Task<bool> CompareAsync(string sourcePath, string targetPath, CancellationToken ct)
	{
		var sourceInfo = new FileInfo(sourcePath);
		var targetInfo = new FileInfo(targetPath);

		IBinaryFileComparer comparer = _selector.Select(sourceInfo, targetInfo);
		return await comparer.CompareAsync(sourceInfo, targetInfo, ct);
	}
}
