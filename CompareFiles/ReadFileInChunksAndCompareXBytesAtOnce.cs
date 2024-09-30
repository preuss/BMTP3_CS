using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.CompareFiles {
	public class ReadFileInChunksAndCompareXBytesAtOnce : ReadIntoByteBufferInChunks {
		public ReadFileInChunksAndCompareXBytesAtOnce(int chunkSize) : base(chunkSize) {
		}
		protected override bool OnCompare(FileInfo fileInfo1, FileInfo fileInfo2) {
			using(var stream1 = fileInfo1.OpenRead())
			using(var stream2 = fileInfo2.OpenRead()) {
				return StreamAreEqual(stream1, stream2);
			}
		}

		private bool StreamAreEqual(in Stream stream1, in Stream stream2) {
			//			const int bufferSize = 1024 * sizeof(Int64);
			byte[] buffer1 = new byte[ChunkSize];
			byte[] buffer2 = new byte[ChunkSize];

			while(true) {
				int count1 = ReadIntoBuffer(stream1, buffer1);
				int count2 = ReadIntoBuffer(stream2, buffer2);

				if(count1 != count2) {
					return false;
				}

				if(count1 == 0) {
					return true;
				}

				// Compare bytes in groups of sizeof(Int64)
				int i = 0;
				for(; i <= count1 - sizeof(Int64); i += sizeof(Int64)) {
					if(BitConverter.ToInt64(buffer1, i) != BitConverter.ToInt64(buffer2, i)) {
						return false;
					}
				}

				// Compare the remaining bytes
				for(; i < count1; i++) {
					if(buffer1[i] != buffer2[i]) {
						return false;
					}
				}
			}
		}
	}
}
