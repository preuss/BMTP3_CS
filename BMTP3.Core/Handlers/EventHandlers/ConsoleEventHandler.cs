using Spectre.Console;
using System.Runtime.InteropServices;

namespace BMTP3.Core.Handlers.EventHandlers {
	public class ConsoleEventHandler {
		private readonly ConsoleEventDelegate _consoleEventDelegate;
		private readonly CancellationTokenSource _cancellationTokenSource;
		// <summary>
		// Always use Spectre.Console instead of System.Console. Spectre.Console is an ANSI IConsole implementation.
		// Keep Console name to force Spectre.Console instead of System.Console usage.
		// </summary>
		private readonly IAnsiConsole Console;

		private delegate bool ConsoleEventDelegate(CtrlType eventType);

		// <summary>
		// https://learn.microsoft.com/en-us/windows/console/setconsolectrlhandler?WT.mc_id=DT-MVP-5003978
		// </summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

		/// <summary>
		/// Initializes a new instance of the <see cref="ConsoleEventHandler"/> class.
		/// </summary>
		/// <param name="cts">The cancellation token source.</param>
		/// <param name="console">The ANSI console instance.</param>
		protected ConsoleEventHandler(CancellationTokenSource cts, IAnsiConsole console) {
			_cancellationTokenSource = cts;
			_consoleEventDelegate = new ConsoleEventDelegate(ConsoleEventCallback);
			Console = console;
		}

		/// <summary>
		/// Initializes the console event handler.
		/// </summary>
		/// <param name="cancellationTokenSource">The cancellation token source.</param>
		/// <param name="console">The ANSI console instance.</param>
		public static void Initialize(CancellationTokenSource cancellationTokenSource, IAnsiConsole? console = default) {
			// Use Spectre.Console instead of System.Console.
			IAnsiConsole Console = console ?? AnsiConsole.Console;

			ConsoleEventHandler consoleEventHandler = new ConsoleEventHandler(cancellationTokenSource, Console);
			consoleEventHandler.Register();
			Console.WriteLine("Application has started. Ctrl-C to end");
		}

		/// <summary>
		/// Registers the console event handler.
		/// </summary>
		public void Register() {
			SetConsoleCtrlHandler(_consoleEventDelegate, true);
			System.Console.CancelKeyPress += CancelKeyPressHandler;
		}

		/// <summary>
		/// Unregisters the console event handler.
		/// </summary>
		public void Unregister() {
			SetConsoleCtrlHandler(_consoleEventDelegate, false);
			System.Console.CancelKeyPress -= CancelKeyPressHandler;
		}

		private void CancelKeyPressHandler(object? sender, ConsoleCancelEventArgs eventArgs) {
			const EventPropagationType CancelEventAndStopPropagation = EventPropagationType.StopPropagation;

			Console.WriteLine("Cancel event triggered");
			if(eventArgs.SpecialKey == ConsoleSpecialKey.ControlC) {
				Console.WriteLine("Ctrl+C was pressed");
			} else if(eventArgs.SpecialKey == ConsoleSpecialKey.ControlBreak) {
				Console.WriteLine("Ctrl+Break was pressed");
			}

			Console.MarkupLine("[red]Operation cancelled![/]");
			_cancellationTokenSource.Cancel();
			eventArgs.Cancel = EventPropagationType.StopPropagation == CancelEventAndStopPropagation;
		}

		// https://learn.microsoft.com/en-us/windows/console/handlerroutine?WT.mc_id=DT-MVP-5003978
		private bool ConsoleEventCallback(CtrlType eventType) {
			var cursorPos = System.Console.GetCursorPosition();
			Console.WriteLine();
			Console.WriteLine();
			Console.Cursor.SetPosition(0, cursorPos.Top + 2);
			//System.Console.SetCursorPosition(0, cursorPos.Top + 3);

			EventPropagationType propagationType;
			Console.WriteLine($"{eventType} detected!");
			switch(eventType) {
				case CtrlType.CTRL_CLOSE_EVENT:
				case CtrlType.CTRL_LOGOFF_EVENT:
				case CtrlType.CTRL_SHUTDOWN_EVENT:
					propagationType = HandleShutdownEvent(eventType);
					break;
				case CtrlType.CTRL_C_EVENT:
				case CtrlType.CTRL_BREAK_EVENT:
					propagationType = HandleCtrlEvent(eventType);
					break;
				default:
					// Unhandled event
					propagationType = EventPropagationType.ContinuePropagation;
					break;
			}
			return propagationType == EventPropagationType.StopPropagation;
		}

		private EventPropagationType HandleShutdownEvent(CtrlType eventType) {
			// Implement graceful shutdown logic here
			return EventPropagationType.ContinuePropagation;
		}
		private EventPropagationType HandleCtrlEvent(CtrlType eventType) {
			Console.WriteLine("We will deal with this another place!");
			// Implement graceful cancel & break logic here

			_cancellationTokenSource.Cancel();
			return EventPropagationType.StopPropagation;
		}
	}
}