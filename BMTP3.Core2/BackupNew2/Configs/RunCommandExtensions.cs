using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.Configs {
	public static class RunCommandExtensions {
		public static bool IsBackup(this RunCommand runCommand) {
			return RunCommand.BACKUP == runCommand;
		}
	}
}
