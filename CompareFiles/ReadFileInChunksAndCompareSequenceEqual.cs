using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;

namespace BMTP3_CS.CompareFiles {
	public class ReadFileInChunksAndCompareSequenceEqual : ReadIntoByteBufferInChunks {
		public ReadFileInChunksAndCompareSequenceEqual(string filePath01, string filePath02, int chunkSize)
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

			try {
				while(true) {
					int count1 = ReadIntoBuffer(stream1, buffer1);
					int count2 = ReadIntoBuffer(stream2, buffer2);

					if(count1 != count2) {
						return false;
					}

					if(count1 == 0) {
						return true;
					}

					if(count1 == ChunkSize) {
						if(!buffer1.SequenceEqual(buffer2)) {
							return false;
						}
					} else {
						if(!buffer1.Take(count1).SequenceEqual(buffer2.Take(count1))) {
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
