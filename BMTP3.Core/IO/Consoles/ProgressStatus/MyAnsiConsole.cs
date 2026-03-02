using Spectre.Console;

namespace BMTP3.Core.IO.Consoles.ProgressStatus {
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