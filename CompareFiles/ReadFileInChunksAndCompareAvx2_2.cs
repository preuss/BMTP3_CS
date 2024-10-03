using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;
using System.IO;
using System.Diagnostics;

namespace BMTP3_CS.CompareFiles {
	public class ReadFileInChunksAndCompareAvx2_2 : ReadFileInChunks {
		public ReadFileInChunksAndCompareAvx2_2(int chunkSize)
			: base(chunkSize) {
		}
		protected override bool OnCompare(FileInfo fileInfo1, FileInfo fileInfo2) {
			using(var stream1 = fileInfo1.OpenRead())
			using(var stream2 = fileInfo2.OpenRead()) {
				return StreamAreEqual(stream1, stream2);
			}
		}
		private unsafe bool StreamAreEqual(in Stream stream1, in Stream stream2) {
			ArrayPool<byte> sharedArrayPool = ArrayPool<byte>.Shared;
			byte[] buffer1 = sharedArrayPool.Rent(ChunkSize);
			byte[] buffer2 = sharedArrayPool.Rent(ChunkSize);
			Array.Fill<byte>(buffer1, 0);
			Array.Fill<byte>(buffer2, 0);
			int vectorSize = Vector256<byte>.Count;
			try {
				while(true) {
					int len1 = 0;
					for(int read;
					len1 < buffer1.Length &&
						 (read = stream1.Read(buffer1, len1, buffer1.Length - len1)) != 0;
						 len1 += read) {
					}

					int len2 = 0;
					for(int read;
					len2 < buffer1.Length &&
						 (read = stream2.Read(buffer2, len2, buffer2.Length - len2)) != 0;
						 len2 += read) {
					}

					if(len1 != len2) {
						return false;
					}

					if(len1 == 0) {
						return true;
					}

					unsafe {
						fixed(byte* pb1 = buffer1) {
							fixed(byte* pb2 = buffer2) {
								int processed;
								for(processed = 0; processed < len1; processed += vectorSize) {
									Vector256<byte> result = Avx2.CompareEqual(Avx.LoadVector256(pb1 + processed), Avx.LoadVector256(pb2 + processed));
									if(Avx2.MoveMask(result) != -1) {
										return false;
									}
								}
								// Compare the remaining bytes
								for(int i = processed; i < len1; i++) {
									if(buffer1[i] != buffer2[i]) {
										return false;
									}
								}
							}
						}
					}
				}
			} finally {
				sharedArrayPool.Return(buffer1);
				sharedArrayPool.Return(buffer2);
			}
		}
	}
}
