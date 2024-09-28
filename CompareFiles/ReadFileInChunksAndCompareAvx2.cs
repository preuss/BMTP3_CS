using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;

namespace BMTP3_CS.CompareFiles {
	public class ReadFileInChunksAndCompareAvx2 : ReadIntoByteBufferInChunks {
		public ReadFileInChunksAndCompareAvx2(string filePath01, string filePath02, int chunkSize)
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
			//var buffer1 = new byte[ChunkSize];
			//var buffer2 = new byte[ChunkSize];

			Int32 vectorSize = Vector256<byte>.Count;

			try {
				fixed(byte* oh1 = buffer1) {
					fixed(byte* oh2 = buffer2) {
						while(true) {
							var count1 = ReadIntoBuffer(stream1, buffer1);
							var count2 = ReadIntoBuffer(stream2, buffer2);

							if(count1 != count2) {
								return false;
							}

							if(count1 == 0) {
								return true;
							}

							var totalProcessed = 0;
							while(totalProcessed <= (count1 - vectorSize)) {
								var result = Avx2.CompareEqual(Avx.LoadVector256(oh1 + totalProcessed), Avx.LoadVector256(oh2 + totalProcessed));
								if(Avx2.MoveMask(result) != -1) {
									return false;
								}
								//Int32 length = Math.Min(vectorSize, count1 - totalProcessed);
								totalProcessed += vectorSize;
							}
							// Compare the rest of the bytes. If there are any.
							for(int i = totalProcessed; i < count1; i++) {
								if(buffer1[i] != buffer2[i]) {
									return false;
								}
							}
						}
					}
				}
			} finally {
				sharedArrayPool.Return(buffer1);
				sharedArrayPool.Return(buffer2);
			}
		}
	}
}
