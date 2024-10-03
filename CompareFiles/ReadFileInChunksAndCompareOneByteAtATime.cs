using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.CompareFiles {
	public class ReadFileInChunksAndCompareOneByteAtATime : ReadFileInChunks {
		public ReadFileInChunksAndCompareOneByteAtATime(int chunkSize)
			: base(chunkSize) {
		}
		protected override bool OnCompare(FileInfo fileInfo1, FileInfo fileInfo2) {
			using(var stream1 = fileInfo1.OpenRead())
			using(var stream2 = fileInfo2.OpenRead()) {
				return StreamAreEqual(stream1, stream2);
			}
		}
		private bool StreamAreEqual(in Stream stream1, in Stream stream2) {
			byte[] buffer1 = new byte[ChunkSize];
			byte[] buffer2 = new byte[ChunkSize];

			while(true) {
				int numBytesRead1 = ReadIntoBuffer(stream1, buffer1);
				int numBytesRead2 = ReadIntoBuffer(stream2, buffer2);

				if(numBytesRead1 != numBytesRead2) {
					return false;
				}

				if(numBytesRead1 == 0) {
					return true;
				}

				// Compare one byte at a time.
				for(int i = 0; i < numBytesRead1; i++) {
					if(buffer1[i] != buffer2[i]) {
						return false;
					}
				}
			}
		}
	}
}
