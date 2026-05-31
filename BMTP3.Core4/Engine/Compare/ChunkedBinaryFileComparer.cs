using System.Buffers;

namespace BMTP3.Core4.Engine.Compare;

internal abstract class ChunkedBinaryFileComparer : BinaryFileComparerBase
{
	private const int DefaultChunkSize = 64 * 1024;

	protected int ChunkSize { get; }

	protected ChunkedBinaryFileComparer(int chunkSize = DefaultChunkSize)
	{
		ChunkSize = chunkSize > 0 ? chunkSize : DefaultChunkSize;
	}

	protected override async Task<bool> OnCompareAsync(FileInfo sourceInfo, FileInfo targetInfo, CancellationToken ct)
	{
		await using FileStream sourceStream = sourceInfo.OpenRead();
		await using FileStream targetStream = targetInfo.OpenRead();

		byte[] buffer1 = ArrayPool<byte>.Shared.Rent(ChunkSize);
		byte[] buffer2 = ArrayPool<byte>.Shared.Rent(ChunkSize);

		try
		{
			while(true)
			{
				ct.ThrowIfCancellationRequested();

				int bytesRead1 = await sourceStream.ReadAsync(buffer1.AsMemory(0, ChunkSize), ct);
				int bytesRead2 = await targetStream.ReadAsync(buffer2.AsMemory(0, ChunkSize), ct);

				if(bytesRead1 != bytesRead2)
					return false;

				if(bytesRead1 == 0)
					return true;

				if(!AreBuffersEqual(buffer1.AsSpan(0, bytesRead1), buffer2.AsSpan(0, bytesRead2)))
					return false;
			}
		} finally
		{
			ArrayPool<byte>.Shared.Return(buffer1);
			ArrayPool<byte>.Shared.Return(buffer2);
		}
	}

	protected abstract bool AreBuffersEqual(ReadOnlySpan<byte> span1, ReadOnlySpan<byte> span2);
}
