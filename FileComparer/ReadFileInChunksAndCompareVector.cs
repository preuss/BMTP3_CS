using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.FilecCmparer {
	public class ReadFileInChunksAndCompareVector : ReadIntoByteBufferInChunks {
		public ReadFileInChunksAndCompareVector(string filePath01, string filePath02, int chunkSize)
			: base(filePath01, filePath02, chunkSize) {
		}
		protected override bool OnCompare() {
			using(var stream1 = FileInfo1.OpenRead())
			using(var stream2 = FileInfo2.OpenRead()) {
				return StreamAreEqual(stream1, stream2);
			}
		}
		private bool StreamAreEqual(in Stream stream1, in Stream stream2) {
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

				if(false == Vector.EqualsAll(new Vector<byte>(buffer1), new Vector<byte>(buffer2))) {
					return false;
				}
			}
		}
	}
}
