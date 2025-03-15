using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Exceptions {
	public class BackupException : Exception {
		public BackupException(string message) : base(message) { }
		public BackupException(string message, Exception innerException) : base(message, innerException) { }
	}
}
