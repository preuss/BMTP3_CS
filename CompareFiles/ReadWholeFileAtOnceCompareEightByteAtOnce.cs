using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.CompareFiles {
	public class ReadWholeFileAtOnceCompareEightByteAtOnce : FileComparer {
		public ReadWholeFileAtOnceCompareEightByteAtOnce() : base() {
		}
		protected override bool OnCompare(FileInfo fileInfo1, FileInfo fileInfo2) {
			byte[] fileContents01 = File.ReadAllBytes(fileInfo1.FullName);
			byte[] fileContents02 = File.ReadAllBytes(fileInfo2.FullName);

			// Verify if files have same length.
			if(fileContents01.Length != fileContents02.Length) {
				return false;
			}

			int lastBlockIndex = fileContents01.Length - (fileContents01.Length % sizeof(ulong));

			int totalProcessed = 0;
			while(totalProcessed < lastBlockIndex) {
				if(BitConverter.ToUInt64(fileContents01, totalProcessed) != BitConverter.ToUInt64(fileContents02, totalProcessed)) {
					return false;
				}
				totalProcessed += sizeof(ulong);
			}

			// Compare last block
			for(int i = totalProcessed; i < fileContents01.Length; i++) {
				if(fileContents01[i] != fileContents02[i]) {
					return false;
				}
			}

			return true;
		}
	}
}
