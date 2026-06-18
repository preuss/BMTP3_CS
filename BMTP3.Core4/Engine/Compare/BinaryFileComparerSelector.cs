using BMTP3.Core4.Engine.Compare.Algorithms;
using System.Runtime.Intrinsics;

namespace BMTP3.Core4.Engine.Compare;

internal sealed class BinaryFileComparerSelector
{
	private const long WholeFileThreshold = 10 * 1024 * 1024; // 10 MB

	private readonly WholeFileSequenceEqualBinaryComparer _wholeFile;
	private readonly ChunkedSequenceEqualBinaryComparer _chunkedDefault;
	private readonly ChunkedVectorBinaryComparer _chunkedVector;
	private readonly ChunkedEightByteBinaryComparer _chunkedEightByte;
	private readonly ChunkedAvx2BinaryComparer _chunkedAvx2;

	public BinaryFileComparerSelector(
		WholeFileSequenceEqualBinaryComparer wholeFile,
		ChunkedSequenceEqualBinaryComparer chunkedDefault,
		ChunkedVectorBinaryComparer chunkedVector,
		ChunkedEightByteBinaryComparer chunkedEightByte,
		ChunkedAvx2BinaryComparer chunkedAvx2)
	{
		_wholeFile = wholeFile ?? throw new ArgumentNullException(nameof(wholeFile));
		_chunkedDefault = chunkedDefault ?? throw new ArgumentNullException(nameof(chunkedDefault));
		_chunkedVector = chunkedVector ?? throw new ArgumentNullException(nameof(chunkedVector));
		_chunkedEightByte = chunkedEightByte ?? throw new ArgumentNullException(nameof(chunkedEightByte));
		_chunkedAvx2 = chunkedAvx2 ?? throw new ArgumentNullException(nameof(chunkedAvx2));
	}

	public IBinaryFileComparer Select(FileInfo sourceInfo, FileInfo targetInfo)
	{
		ArgumentNullException.ThrowIfNull(sourceInfo);
		ArgumentNullException.ThrowIfNull(targetInfo);

		sourceInfo.Refresh();
		if(!sourceInfo.Exists)
			throw new ArgumentException($"File does not exist: {sourceInfo.FullName}", nameof(sourceInfo));
		targetInfo.Refresh();
		if(!targetInfo.Exists)
			throw new ArgumentException($"File does not exist: {targetInfo.FullName}", nameof(targetInfo));

		long maxLength = Math.Max(sourceInfo.Length, targetInfo.Length);

		if(maxLength <= WholeFileThreshold)
			return _wholeFile;

		if(_chunkedAvx2.GetType().Assembly != null && System.Runtime.Intrinsics.X86.Avx2.IsSupported)
			return _chunkedAvx2;

		if(Vector256.IsHardwareAccelerated)
			return _chunkedAvx2;

		if(Vector128.IsHardwareAccelerated)
			return _chunkedVector;

		if(IntPtr.Size == 8)
			return _chunkedEightByte;

		return _chunkedDefault;
	}
}
