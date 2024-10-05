using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;
using System.Runtime.InteropServices;

namespace BMTP3_CS.CompareFiles {
	/// <summary>
	/// Fastest 1024 KB
	/// But 512 KB almost the same.
	/// </summary>
	public class ReadFileInChunksAndCompareMemCmp : ReadFileInChunks {
		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int memcmp(byte[] b1, byte[] b2, long count);

		public ReadFileInChunksAndCompareMemCmp(int chunkSize)
			: base(chunkSize) {
		}

		protected override bool OnCompare(FileInfo fileInfo1, FileInfo fileInfo2) {
			using(var stream1 = fileInfo1.OpenRead())
			using(var stream2 = fileInfo2.OpenRead()) {
				return StreamAreEqual(stream1, stream2);
			}
		}

		private bool StreamAreEqual(Stream stream1, Stream stream2) {
			ArrayPool<byte> sharedArrayPool = ArrayPool<byte>.Shared;
			byte[] buffer1 = sharedArrayPool.Rent(ChunkSize);
			byte[] buffer2 = sharedArrayPool.Rent(ChunkSize);

			try {
				while(true) {
					var count1 = ReadIntoBuffer(stream1, buffer1);
					var count2 = ReadIntoBuffer(stream2, buffer2);

					if(count1 != count2) {
						return false;
					}

					if(count1 == 0) {
						return true;
					}

					if(memcmp(buffer1, buffer2, count1) != 0) {
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
