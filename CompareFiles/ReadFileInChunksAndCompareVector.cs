using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.CompareFiles {
	public class ReadFileInChunksAndCompareVector : ReadIntoByteBufferInChunks {
		public ReadFileInChunksAndCompareVector(int chunkSize)
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

			Int32 vectorSize = Vector<byte>.Count;

			while(true) {
				int count1 = ReadIntoBuffer(stream1, buffer1);
				int count2 = ReadIntoBuffer(stream2, buffer2);

				if(count1 != count2) {
					return false;
				}

				if(count1 == 0) {
					return true;
				}
				int i;
				for(i = 0; i <= (count1 - vectorSize); i += vectorSize) {
					if(false == Vector.EqualsAll(new Vector<byte>(buffer1, i), new Vector<byte>(buffer2, i))) {
						return false;
					}
				}
				// Compare the rest of the bytes. If there are any.
				for(; i < count1; i++) {
					if(buffer1[i] != buffer2[i]) {
						return false;
					}
				}
			}
		}
	}
}
