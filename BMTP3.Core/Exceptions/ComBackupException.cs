using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Exceptions {
	public class ComBackupException : BackupException {
		public ComBackupException(string message) : base(message) { }
		public ComBackupException(string message, Exception innerException) : base(message, innerException) { }
	}
}
