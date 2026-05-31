namespace BMTP3.Core4.Engine.Compare;

internal interface IFileCompareService
{
	Task<bool> CompareAsync(
		string sourcePath,
		string targetPath,
		CancellationToken ct
	);
}

