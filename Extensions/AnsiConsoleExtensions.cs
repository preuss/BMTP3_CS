using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Extensions {
	public static class AnsiConsoleExtensions {
		public static void AddCancelKeyPressHandler(this IAnsiConsole console, ConsoleCancelEventHandler handler) => Console.CancelKeyPress += handler;

		public static void RemoveCancelKeyPressHandler(this IAnsiConsole console, ConsoleCancelEventHandler handler) => Console.CancelKeyPress -= handler;

		public static int GetCursorTop() {
			return System.Console.CursorTop;
		}
		public static int GetWindowHeight() {
			return System.Console.WindowHeight;
		}
	}
}
