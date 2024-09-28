using BMTP3_CS.CompareFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.CompareFiles {
	public class ReadFileInChunksAndCompareVector2 : ReadIntoByteBufferInChunks {
		public ReadFileInChunksAndCompareVector2(string filePath1, string filePath2, int chunkSize)
			: base(filePath1, filePath2, chunkSize) {
		}

		protected override bool OnCompare() {
			if(FileInfo1.Length != FileInfo2.Length) {
				return false;
			}
			using(var file1 = FileInfo1.OpenRead())
			using(var file2 = FileInfo2.OpenRead()) {
				return StreamsContentsAreEqual(file1, file2);
			}
		}

		private bool StreamsContentsAreEqual(Stream stream1, Stream stream2) {
			var buffer1 = new byte[ChunkSize];
			var buffer2 = new byte[ChunkSize];
			var tasks = new List<Task<bool>>();

			while(true) {
				int count1 = ReadIntoBuffer(stream1, buffer1);
				int count2 = ReadIntoBuffer(stream2, buffer2);

				if(count1 != count2) {
					return false;
				}

				if(count1 == 0) {
					Task.WaitAll(tasks.ToArray());
					return tasks.All(t => t.Result);
				}

				var buffer1Copy = (byte[])buffer1.Clone();
				var buffer2Copy = (byte[])buffer2.Clone();

				var task = Task.Run(() =>
				{
					for(int i = 0; i < count1; i += Vector<byte>.Count) {
						var vector1 = new Vector<byte>(buffer1Copy, i);
						var vector2 = new Vector<byte>(buffer2Copy, i);

						if(!Vector.EqualsAll(vector1, vector2)) {
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
