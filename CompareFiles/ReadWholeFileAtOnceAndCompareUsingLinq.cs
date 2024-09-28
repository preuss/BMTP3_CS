using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.CompareFiles {
	public class ReadWholeFileAtOnceAndCompareUsingLinq : FileComparer {
		public ReadWholeFileAtOnceAndCompareUsingLinq(string filePath01, string filePath02) : base(filePath01, filePath02) {
		}
		protected override bool OnCompare() {
			byte[] fileContents01 = File.ReadAllBytes(FileInfo1.FullName);
			byte[] fileContents02 = File.ReadAllBytes(FileInfo2.FullName);
			return !fileContents01.Where((t, i) => t != fileContents02[i]).Any();
		}
	}
	public class ReadWholeFileAtOnceAndUseSequenceEquals : FileComparer {
		public ReadWholeFileAtOnceAndUseSequenceEquals(string filePath01, string filePath02) : base(filePath01, filePath02) {
		}
		protected override bool OnCompare() {
			byte[] fileContents01 = File.ReadAllBytes(FileInfo1.FullName);
			byte[] fileContents02 = File.ReadAllBytes(FileInfo2.FullName);
			return fileContents01.SequenceEqual(fileContents02);
		}
	}
}
