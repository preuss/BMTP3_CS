namespace BMTP3.Core.IO.Consoles.ProgressStatus {
	/// <summary>
	/// Contains extension methods for <see cref="IAnsiConsole"/>.
	/// </summary>
	public static partial class AnsiConsoleExtensions {
		/// <summary>
		/// Creates a new <see cref="Progress"/> instance for the console.
		/// </summary>
		/// <param name="console">The console.</param>
		/// <returns>A <see cref="Progress"/> instance.</returns>
		public static ProgressStatus ProgressStatus(this Spectre.Console.IAnsiConsole console) {
			ArgumentNullException.ThrowIfNull(console);

			return new ProgressStatus(console);
		}
	}
}
