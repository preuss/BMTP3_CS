using BMTP3_CS.CompareFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.CompareFiles {
	public class ReadFileInChunksAndCompareVector2 : ReadIntoByteBufferInChunks {
		public ReadFileInChunksAndCompareVector2(int chunkSize)
			: base(chunkSize) {
		}

		protected override bool OnCompare(FileInfo fileInfo1, FileInfo fileInfo2) {
			using(var file1 = fileInfo1.OpenRead())
			using(var file2 = fileInfo2.OpenRead()) {
				return StreamsContentsAreEqual(file1, file2);
			}
		}

		private bool StreamsContentsAreEqual(Stream stream1, Stream stream2) {
			var buffer1 = new byte[ChunkSize];
			var buffer2 = new byte[ChunkSize];
			var tasks = new List<Task<bool>>();

			while(true) {
				int numBytesRead1 = ReadIntoBuffer(stream1, buffer1);
				int numBytesRead2 = ReadIntoBuffer(stream2, buffer2);

				if(numBytesRead1 != numBytesRead2) {
					return false;
				}

				if(numBytesRead1 == 0) {
					Task.WaitAll(tasks.ToArray());
					return tasks.All(t => t.Result);
				}

				var buffer1Copy = (byte[])buffer1.Clone();
				var buffer2Copy = (byte[])buffer2.Clone();

				var task = Task.Run(() =>
				{
					int i = 0;
					for(; i <= (numBytesRead1 - Vector<byte>.Count); i += Vector<byte>.Count) {
						var vector1 = new Vector<byte>(buffer1Copy, i);
						var vector2 = new Vector<byte>(buffer2Copy, i);

						if(!Vector.EqualsAll(vector1, vector2)) {
							return false;
						}
					}
					// Compare the rest of the bytes. If there are any.
					for(; i < numBytesRead1; i++) {
						if(buffer1Copy[i] != buffer2Copy[i]) {
							return false;
						}
					}
					return true;
				});

				tasks.Add(task);
			}
		}
	}
}
