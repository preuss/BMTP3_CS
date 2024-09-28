using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;

namespace BMTP3_CS.CompareFiles {
	public class ReadFileInChunksAndCompareEightByteAtOnce : ReadIntoByteBufferInChunks {
		public ReadFileInChunksAndCompareEightByteAtOnce(string filePath01, string filePath02, int chunkSize)
			: base(filePath01, filePath02, chunkSize) {
		}
		protected override bool OnCompare() {
			using(var stream1 = FileInfo1.OpenRead())
			using(var stream2 = FileInfo2.OpenRead()) {
				return StreamAreEqual(stream1, stream2);
			}
		}
		private bool StreamAreEqual(in Stream stream1, in Stream stream2) {
			ArrayPool<byte> sharedArrayPool = ArrayPool<byte>.Shared;
			byte[] buffer1 = sharedArrayPool.Rent(ChunkSize);
			byte[] buffer2 = sharedArrayPool.Rent(ChunkSize);
			//var buffer1 = new byte[ChunkSize];
			//var buffer2 = new byte[ChunkSize];
			var sizeOfInt64 = sizeof(Int64);

			try {
				int bytesRead1;
				int bytesRead2;
				while(true) {
					bytesRead1 = ReadIntoBuffer(stream1, buffer1);
					bytesRead2 = ReadIntoBuffer(stream2, buffer2);
					if(bytesRead1 != bytesRead2) {
						return false;
					}

					if(bytesRead1 == 0) {
						return true;
					}

					for(int i = 0; i <= (bytesRead1 - sizeOfInt64); i += sizeOfInt64) {
						if(BitConverter.ToInt64(buffer1, i) != BitConverter.ToInt64(buffer2, i)) {
							return false;
						}
					}
					// Handle the rest of the bytes.
					for(int i = bytesRead1 - (bytesRead1 % sizeOfInt64); i < bytesRead1; i++) {
						if(buffer1[i] != buffer2[i]) {
							return false;
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
