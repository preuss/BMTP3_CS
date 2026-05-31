using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace BMTP3.Core4.Engine.Compare.Algorithms;

internal sealed class ChunkedAvx2BinaryComparer : ChunkedBinaryFileComparer
{
	public ChunkedAvx2BinaryComparer() : base(chunkSize: 256 * 1024)
	{
	}

	protected override bool AreBuffersEqual(ReadOnlySpan<byte> span1, ReadOnlySpan<byte> span2)
	{
		if(!Avx2.IsSupported)
		{
			return span1.SequenceEqual(span2);
		}

		int vectorSize = Vector256<byte>.Count;
		int i = 0;

		for(; i <= span1.Length - vectorSize; i += vectorSize)
		{
			Vector256<byte> v1 = Vector256.Create<byte>(span1.Slice(i));
			Vector256<byte> v2 = Vector256.Create<byte>(span2.Slice(i));
			Vector256<byte> cmp = Avx2.CompareEqual(v1, v2);
			if(!Avx2.MoveMask(cmp).Equals(-1))
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
