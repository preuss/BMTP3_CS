using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.CompareFiles {
	public class ReadWholeFileAtOnce : FileComparer {
		public ReadWholeFileAtOnce() : base() {
		}
		protected override bool OnCompare(FileInfo fileInfo1, FileInfo fileInfo2) {
			byte[] fileContents01 = File.ReadAllBytes(fileInfo1.FullName);
			byte[] fileContents02 = File.ReadAllBytes(fileInfo2.FullName);
			for(var i = 0; i < fileContents01.Length; i++) {
				if(fileContents01[i] != fileContents02[i]) {
					return false;
				}
			}
			return true;
		}
	}
}
