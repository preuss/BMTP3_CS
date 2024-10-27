using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.CompareFiles {
	public class ReadWholeFileAtOnceAndCompareUsingLinq : FileComparer {
		public ReadWholeFileAtOnceAndCompareUsingLinq() : base() {
		}
		protected override bool OnCompare(FileInfo fileInfo1, FileInfo fileInfo2) {
			byte[] fileContents01 = File.ReadAllBytes(fileInfo1.FullName);
			byte[] fileContents02 = File.ReadAllBytes(fileInfo2.FullName);
			return !fileContents01.Where((t, i) => t != fileContents02[i]).Any();
		}
	}
}
