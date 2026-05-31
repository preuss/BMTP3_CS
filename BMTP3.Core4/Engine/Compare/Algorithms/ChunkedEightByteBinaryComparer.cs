namespace BMTP3.Core4.Engine.Compare.Algorithms;

internal sealed class ChunkedEightByteBinaryComparer : ChunkedBinaryFileComparer
{
	protected override bool AreBuffersEqual(ReadOnlySpan<byte> span1, ReadOnlySpan<byte> span2)
	{
		ReadOnlySpan<long> long1 = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, long>(span1);
		ReadOnlySpan<long> long2 = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, long>(span2);

		for(int i = 0; i < long1.Length; i++)
		{
			if(long1[i] != long2[i])
				return false;
		}

		int remainingStart = long1.Length * sizeof(long);
		for(int i = remainingStart; i < span1.Length; i++)
		{
			if(span1[i] != span2[i])
				return false;
		}

		return true;
	}
}
