using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.CompareFiles {
	public class ReadWholeFileAtOnceCompareEightByteAtOnce : FileComparer {
		public ReadWholeFileAtOnceCompareEightByteAtOnce(string filePath01, string filePath02) : base(filePath01, filePath02) {
		}
		protected override bool OnCompare() {
			byte[] fileContents01 = File.ReadAllBytes(FileInfo1.FullName);
			byte[] fileContents02 = File.ReadAllBytes(FileInfo2.FullName);

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
