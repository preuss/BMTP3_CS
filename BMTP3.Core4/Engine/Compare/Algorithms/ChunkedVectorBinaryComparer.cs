using System.Numerics;

namespace BMTP3.Core4.Engine.Compare.Algorithms;

internal sealed class ChunkedVectorBinaryComparer : ChunkedBinaryFileComparer
{
	protected override bool AreBuffersEqual(ReadOnlySpan<byte> span1, ReadOnlySpan<byte> span2)
	{
		int vectorSize = Vector<byte>.Count;
		int i = 0;

		for(; i <= span1.Length - vectorSize; i += vectorSize)
		{
			Vector<byte> v1 = new(span1.Slice(i));
			Vector<byte> v2 = new(span2.Slice(i));
			if(!Vector.EqualsAll(v1, v2))
				return false;
		}

		for(; i < span1.Length; i++)
		{
			if(span1[i] != span2[i])
				return false;
		}

		return true;
	}
}
