using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Exceptions {
	internal class BackupConfigFileNotFoundException : Exception {
		public BackupConfigFileNotFoundException(string message) : base(message) {
		}
		public BackupConfigFileNotFoundException(string message, Exception inner) : base(message, inner) {
		}
	}
}
