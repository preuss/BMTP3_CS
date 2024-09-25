using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Exceptions {
	internal class BackupConfigFileNotFoundException : Exception {
		public BackupConfigFileNotFoundException(string message) : base(message) {
		}
	}
}
