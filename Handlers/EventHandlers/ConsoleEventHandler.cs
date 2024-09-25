using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3_CS.Handlers.EventHandlers {
	public class ConsoleEventHandler {
		private readonly ConsoleEventDelegate _consoleEventDelegate;
		private readonly CancellationTokenSource _cancellationTokenSource;

		// Always use Spectre.Console instead of System.Console. Spectre.Console is an ANSI IConsole implementation.
		// Keep Console name to force Spectre.Console instead of System.Console usage.
		private readonly IAnsiConsole Console;

		private delegate bool ConsoleEventDelegate(CtrlType eventType);

		// https://learn.microsoft.com/en-us/windows/console/setconsolectrlhandler?WT.mc_id=DT-MVP-5003978
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

		public enum CtrlType {
			CTRL_C_EVENT = 0,
			CTRL_BREAK_EVENT = 1,
			CTRL_CLOSE_EVENT = 2,
			CTRL_LOGOFF_EVENT = 5,
			CTRL_SHUTDOWN_EVENT = 6
		}

		protected ConsoleEventHandler(CancellationTokenSource cts, IAnsiConsole console) {
			this._cancellationTokenSource = cts;
			_consoleEventDelegate = new ConsoleEventDelegate(ConsoleEventCallback);
			Console = console;
		}

		public static void Initialize(CancellationTokenSource cancellationTokenSource, IAnsiConsole? console = default) {
			// Use Spectre.Console instead of System.Console.
			IAnsiConsole Console = console ?? AnsiConsole.Console;

			ConsoleEventHandler consoleEventHandler = new ConsoleEventHandler(cancellationTokenSource, Console);
			consoleEventHandler.Register();
			Console.WriteLine("Application has started. Ctrl-C to end");
		}
		public void Register() {
			SetConsoleCtrlHandler(_consoleEventDelegate, true);
			System.Console.CancelKeyPress += CancelKeyPressHandler;
		}

		public void Unregister() {
			SetConsoleCtrlHandler(_consoleEventDelegate, false);
			System.Console.CancelKeyPress -= CancelKeyPressHandler;
		}

		private void CancelKeyPressHandler(object? sender, ConsoleCancelEventArgs eventArgs) {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
			// Keep constants to make the code more understandable.
			const bool CANCEL_EVENT_AND_STOP_PROPAGATION = true;
			const bool CONTINUE_EVENT_PROPAGATION = false;
#pragma warning restore CS0219 // Variable is assigned but its value is never used

			Console.WriteLine("Cancel event triggered");
			if(eventArgs.SpecialKey == ConsoleSpecialKey.ControlC) {
				Console.WriteLine("Ctrl+C was pressed");
			} else if(eventArgs.SpecialKey == ConsoleSpecialKey.ControlBreak) {
				Console.WriteLine("Ctrl+Break was pressed");
			}

			Console.MarkupLine("[red]Operation cancelled![/]");
			_cancellationTokenSource.Cancel();
			eventArgs.Cancel = CANCEL_EVENT_AND_STOP_PROPAGATION;
		}

		// https://learn.microsoft.com/en-us/windows/console/handlerroutine?WT.mc_id=DT-MVP-5003978
		private bool ConsoleEventCallback(CtrlType eventType) {
			// Keep constants to make the code more understandable.
			const bool HANDLED_EVENT_AND_STOP_PROPAGATING = true;
			const bool UNHANDLED_EVENT_AND_CONTINUE_PROPAGATING = false;

			var cursorPos = System.Console.GetCursorPosition();
			Console.WriteLine();
			Console.WriteLine();
			System.Console.SetCursorPosition(0, cursorPos.Top + 3);

			switch(eventType) {
				case CtrlType.CTRL_CLOSE_EVENT:
				case CtrlType.CTRL_LOGOFF_EVENT:
				case CtrlType.CTRL_SHUTDOWN_EVENT:
					Console.WriteLine($"{eventType} detected!");
					// Implement graceful shutdown logic here
					break;
				case CtrlType.CTRL_C_EVENT:
				case CtrlType.CTRL_BREAK_EVENT:
					Console.WriteLine($"{eventType} detected!");
					Console.WriteLine("We will deal with this another place!");

					_cancellationTokenSource.Cancel();
					return HANDLED_EVENT_AND_STOP_PROPAGATING;
			}
			return UNHANDLED_EVENT_AND_CONTINUE_PROPAGATING;
		}
	}
}