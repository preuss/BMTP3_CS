using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;

namespace BMTP3_CS.CompareFiles {
	/// <summary>
	/// The fastest of all the along with 1024 KB
	/// But 512 KB just after in speed.
	/// </summary>
	public class ReadFileInChunksAndCompareSequenceEqual : ReadFileInChunks {
		/// <summary>
		/// 1024 KB is fastest
		/// 512 KB is second fastest
		/// </summary>
		/// <param name="chunkSize"></param>
		public ReadFileInChunksAndCompareSequenceEqual(int chunkSize)
			: base(chunkSize) {
		}
		protected override bool OnCompare(FileInfo fileInfo1, FileInfo fileInfo2) {
			using(var stream1 = fileInfo1.OpenRead())
			using(var stream2 = fileInfo2.OpenRead()) {
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

					if(!buffer1.AsSpan(0, count1).SequenceEqual(buffer2.AsSpan(0, count1))) {
						return false;
					}
				}
			} finally {
				sharedArrayPool.Return(buffer1);
				sharedArrayPool.Return(buffer2);
			}
		}
	}
}
