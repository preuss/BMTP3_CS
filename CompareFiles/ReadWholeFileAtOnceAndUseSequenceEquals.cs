using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.CompareFiles {
	public class ReadWholeFileAtOnceAndUseSequenceEquals : FileComparer {
		public ReadWholeFileAtOnceAndUseSequenceEquals() : base() {
		}
		protected override bool OnCompare(FileInfo fileInfo1, FileInfo fileInfo2) {
			byte[] fileContents01 = File.ReadAllBytes(fileInfo1.FullName);
			byte[] fileContents02 = File.ReadAllBytes(fileInfo2.FullName);
			return fileContents01.SequenceEqual(fileContents02);
		}
	}
}
