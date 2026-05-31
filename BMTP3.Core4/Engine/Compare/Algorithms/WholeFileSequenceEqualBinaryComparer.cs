namespace BMTP3.Core4.Engine.Compare.Algorithms;

internal sealed class WholeFileSequenceEqualBinaryComparer : BinaryFileComparerBase
{
	protected override async Task<bool> OnCompareAsync(FileInfo sourceInfo, FileInfo targetInfo, CancellationToken ct)
	{
		byte[] sourceBytes = await File.ReadAllBytesAsync(sourceInfo.FullName, ct);
		byte[] targetBytes = await File.ReadAllBytesAsync(targetInfo.FullName, ct);

		return sourceBytes.AsSpan().SequenceEqual(targetBytes);
	}
}
