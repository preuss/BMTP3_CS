using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.FilecCmparer {
	public abstract class ReadIntoByteBufferInChunks : FileComparer {
		protected readonly int ChunkSize;
		protected ReadIntoByteBufferInChunks(string filePath01, string filePath02, int chunkSize) : base(filePath01, filePath02) {
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
