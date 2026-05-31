namespace BMTP3.Core4.Engine.Compare;

internal abstract class BinaryFileComparerBase : IBinaryFileComparer
{
	public async Task<bool> CompareAsync(FileInfo sourceInfo, FileInfo targetInfo, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(sourceInfo);
		ArgumentNullException.ThrowIfNull(targetInfo);

		sourceInfo.Refresh();
		targetInfo.Refresh();

		if(!sourceInfo.Exists && !targetInfo.Exists)
			return true;

		if(!sourceInfo.Exists || !targetInfo.Exists)
			return false;

		if(string.Equals(sourceInfo.FullName, targetInfo.FullName, StringComparison.OrdinalIgnoreCase))
			return true;

		if(sourceInfo.Length != targetInfo.Length)
			return false;

		if(sourceInfo.Length == 0 && targetInfo.Length == 0)
			return true;

		return await OnCompareAsync(sourceInfo, targetInfo, ct);
	}

	protected abstract Task<bool> OnCompareAsync(FileInfo sourceInfo, FileInfo targetInfo, CancellationToken ct);
}
