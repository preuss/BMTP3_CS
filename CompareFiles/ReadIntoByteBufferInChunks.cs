using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.CompareFiles {
	public abstract class ReadIntoByteBufferInChunks : FileComparer {
		protected readonly int ChunkSize;
		protected ReadIntoByteBufferInChunks(int chunkSize) : base() {
			ChunkSize = chunkSize;
		}
		protected int ReadIntoBuffer(in Stream stream, in byte[] buffer) {
			int bytesRead = 0;
			while(bytesRead < buffer.Length) {
				int read = stream.Read(buffer, bytesRead, buffer.Length - bytesRead);
				// Reached end of stream.
				if(read == 0) {
					return bytesRead;
				}
				bytesRead += read;
			}
			return bytesRead;
		}
	}
}
