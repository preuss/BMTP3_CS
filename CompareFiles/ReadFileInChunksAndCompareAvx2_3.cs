using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;
using System.IO;

namespace BMTP3_CS.CompareFiles {
	public class ReadFileInChunksAndCompareAvx2_3 : ReadIntoByteBufferInChunks {
		public ReadFileInChunksAndCompareAvx2_3(string filePath01, string filePath02, int chunkSize)
			: base(filePath01, filePath02, chunkSize) {
		}
		protected override bool OnCompare() {
			using(var stream1 = FileInfo1.OpenRead())
			using(var stream2 = FileInfo2.OpenRead()) {
				return StreamAreEqual(stream1, stream2);
			}
		}

		private unsafe bool StreamAreEqual(in Stream stream1, in Stream stream2) {
			ArrayPool<byte> sharedArrayPool = ArrayPool<byte>.Shared;
			byte[] buffer1 = sharedArrayPool.Rent(ChunkSize);
			byte[] buffer2 = sharedArrayPool.Rent(ChunkSize);
			Array.Fill<byte>(buffer1, 0);
			Array.Fill<byte>(buffer2, 0);

			try {
				int chunkCount = (int)Math.Ceiling((double)FileInfo1.Length / ChunkSize);
				bool[] results = new bool[chunkCount];
				Parallel.For(0, chunkCount, i => {
					using(var localStream1 = FileInfo1.OpenRead())
					using(var localStream2 = FileInfo2.OpenRead()) {
						long offset = i * ChunkSize;
						int len1 = ReadChunk(localStream1, buffer1, offset);
						int len2 = ReadChunk(localStream2, buffer2, offset);

						if(len1 != len2) {
							results[i] = false;
							return;
						}

						if(len1 == 0) {
							results[i] = true;
							return;
						}

						fixed(byte* pb1 = buffer1)
						fixed(byte* pb2 = buffer2) {
							int vectorSize = Vector256<byte>.Count;
							for(int processed = 0; processed < len1; processed += vectorSize) {
								Vector256<byte> result = Avx2.CompareEqual(Avx.LoadVector256(pb1 + processed), Avx.LoadVector256(pb2 + processed));
								if(Avx2.MoveMask(result) != -1) {
									results[i] = false;
									return;
								}
							}
						}
						results[i] = true;
					}
				});

				return results.All(r => r);
			} finally {
				sharedArrayPool.Return(buffer1);
				sharedArrayPool.Return(buffer2);
			}
		}
		private int ReadChunk(Stream stream, byte[] buffer, long offset) {
			stream.Seek(offset, SeekOrigin.Begin);
			return stream.Read(buffer, 0, buffer.Length);
		}
	}
}
