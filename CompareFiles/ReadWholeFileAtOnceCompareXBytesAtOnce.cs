using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.CompareFiles {
	public class ReadWholeFileAtOnceCompareXBytesAtOnce : ReadIntoByteBufferInChunks {
		public ReadWholeFileAtOnceCompareXBytesAtOnce(string filePath01, string filePath02, int chunkSize) : base(filePath01, filePath02, chunkSize) {
		}
		protected override bool OnCompare() {
			using(var stream1 = FileInfo1.OpenRead())
			using(var stream2 = FileInfo2.OpenRead()) {
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

				for(var i = 0; i < count1; i += sizeof(Int64)) {
					if(BitConverter.ToInt64(buffer1, i) != BitConverter.ToInt64(buffer2, i)) {
						return false;
					}
				}
			}
		}
	}
}
