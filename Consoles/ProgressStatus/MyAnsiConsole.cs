using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

namespace BMTP3_CS.Consoles.ProgressStatus {
	public static partial class MyAnsiConsole {
		/// <summary>
		/// Creates a new <see cref="ProgressStatus"/> instance.
		/// </summary>
		/// <returns>A <see cref="ProgressStatus"/> instance.</returns>
		public static ProgressStatus ProgressStatus() {
			return AnsiConsole.Console.ProgressStatus();
		}
	}
}