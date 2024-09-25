using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.FilecCmparer {
	public class ReadWholeFileAtOnce : FileComparer {
		public ReadWholeFileAtOnce(string filePath01, string filePath02) : base(filePath01, filePath02) {
		}
		protected override bool OnCompare() {
			var fileContents01 = File.ReadAllBytes(FileInfo1.FullName);
			var fileContents02 = File.ReadAllBytes(FileInfo2.FullName);
			for(var i = 0; i < fileContents01.Length; i++) {
				if(fileContents01[i] != fileContents02[i]) {
					return false;
				}
			}
			return true;
		}
	}
}
