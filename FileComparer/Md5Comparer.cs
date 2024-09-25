using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.FilecCmparer {
	public class Md5Comparer : FileComparer {
		public Md5Comparer(string filePath01, string filePath02) : base(filePath01, filePath02) {
		}
		protected override bool OnCompare() {

			using var fileStream01 = FileInfo1.OpenRead();
			using var fileStream02 = FileInfo2.OpenRead();
			using var md5Creator = MD5.Create();

			var fileStream01Hash = md5Creator.ComputeHash(fileStream01);
			var fileStream02Hash = md5Creator.ComputeHash(fileStream02);

			for(var i = 0; i < fileStream01Hash.Length; i++) {
				if(fileStream01Hash[i] != fileStream02Hash[i]) {
					return false;
				}
			}
			return true;
		}
	}
}
