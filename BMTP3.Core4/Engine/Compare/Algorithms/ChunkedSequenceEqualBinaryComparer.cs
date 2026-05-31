namespace BMTP3.Core4.Engine.Compare.Algorithms;

internal sealed class ChunkedSequenceEqualBinaryComparer : ChunkedBinaryFileComparer
{
	protected override bool AreBuffersEqual(ReadOnlySpan<byte> span1, ReadOnlySpan<byte> span2)
		=> span1.SequenceEqual(span2);
}
