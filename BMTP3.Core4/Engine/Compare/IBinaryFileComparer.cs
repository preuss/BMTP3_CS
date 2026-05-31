namespace BMTP3.Core4.Engine.Compare;

internal interface IBinaryFileComparer
{
	Task<bool> CompareAsync(
		FileInfo sourceInfo,
		FileInfo targetInfo,
		CancellationToken ct);
}