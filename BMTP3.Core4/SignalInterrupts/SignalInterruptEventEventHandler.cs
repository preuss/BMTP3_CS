using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace BMTP3.Core4.SignalInterrupts;

/// <summary>
/// Captures OS control signals (Ctrl+C, Ctrl+Break, Window Close, Logoff, Shutdown)
/// via kernel32 SetConsoleCtrlHandler (native thread) and .NET Console.CancelKeyPress
/// (managed thread). Exposes a CancellationToken that is cancelled on any signal,
/// and a SignalReceived event with details about the signal type and characteristics.
///
/// SignalReceived subscribers control behavior via two properties:
/// - <see cref="SignalInterruptEventArgs.StopPropagation"/>: used by kernel32
///   for shutdown events (Close, Logoff, Shutdown). The FIRST subscriber that
///   sets it to true wins — no further handlers called.
/// - <see cref="SignalInterruptEventArgs.SuppressTermination"/>: used by
///   CancelKeyPress for Ctrl+C/Break. ALL subscribers are called; the LAST
///   value wins.
///
/// Flow summary:
/// - Ctrl+C/Break: Kernel32Handler passes through to CancelKeyPress.
///   Subscriber receives event from <see cref="OnCancelKeyPress"/>.
/// - Close/Logoff/Shutdown: Subscriber receives event from
///   <see cref="Kernel32Handler"/> (native thread, ~5 second timeout).
/// </summary>
public sealed class SignalInterruptEventHandler : ISignalInterruptEventHandler
{
	private readonly ILogger<SignalInterruptEventHandler>? _logger;

	private readonly CancellationTokenSource _cancellationTokenSource;
	private readonly ConsoleEventDelegate _kernel32Callback;
	private bool _registered;
	private bool _disposed;

	public event EventHandler<SignalInterruptEventArgs>? SignalReceived;
	public CancellationToken CancellationToken => _cancellationTokenSource.Token;

	private delegate bool ConsoleEventDelegate(CtrlTypes eventType);

	// https://learn.microsoft.com/en-us/windows/console/setconsolectrlhandler
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

	/// <summary>Creates a handler with its own CancellationTokenSource.</summary>
	public SignalInterruptEventHandler(ILogger<SignalInterruptEventHandler>? logger = null) : this(null, logger)
	{
	}

	/// <summary>
	/// Creates a handler whose CancellationToken is linked to an external token.
	/// Use this in e.g. System.CommandLine commands where you have a token from the CLI.
	/// </summary>
	public SignalInterruptEventHandler(CancellationTokenSource? cancellationTokenSource, ILogger<SignalInterruptEventHandler>? logger = null)
	{
		_cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
		_logger = logger;
		_kernel32Callback = Kernel32Handler;
	}

	/// <summary>
	/// Registers SetConsoleCtrlHandler (Windows only) and Console.CancelKeyPress.
	/// Safe to call multiple times — subsequent calls are no-ops.
	/// </summary>
	public void Register()
	{
		if (_registered) return;
		_registered = true;

		if (OperatingSystem.IsWindows())
		{
			SetConsoleCtrlHandler(_kernel32Callback, true);
		}

		System.Console.CancelKeyPress += OnCancelKeyPress;
	}

	/// <summary>
	/// Unregisters handlers and disposes the CTS. Safe to call multiple times.
	/// </summary>
	public void UnRegister()
	{
		if (!_registered) return;
		_registered = false;

		if (OperatingSystem.IsWindows())
		{
			SetConsoleCtrlHandler(_kernel32Callback, false);
		}

		System.Console.CancelKeyPress -= OnCancelKeyPress;
	}

	/// <summary>Unregisters handlers and disposes the CTS. Safe to call multiple times.</summary>
	public void Dispose()
	{
		UnRegister();

		if (_disposed) return;
		_disposed = true;
		_cancellationTokenSource.Cancel();
		_cancellationTokenSource.Dispose();
	}

	/// <summary>
	/// Called by .NET's Console.CancelKeyPress (managed thread pool) for
	/// Ctrl+C and Ctrl+Break events.
	/// <see cref="Kernel32Handler"/> lets these events pass through
	/// (returns false) so the subscriber receives the event here on a
	/// managed thread where async/await, logging, etc. are available.
	///
	/// ALL subscribers are called sequentially. The LAST value of
	/// <see cref="SignalInterruptEventArgs.SuppressTermination"/> wins.
	/// Default is false (allow ExitProcess). Set to true to prevent
	/// ExitProcess and let the app continue via CancellationToken.
	/// </summary>
	private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs)
	{
		CtrlTypes signal = eventArgs.SpecialKey switch
		{
			ConsoleSpecialKey.ControlBreak => CtrlTypes.CTRL_BREAK_EVENT,
			ConsoleSpecialKey.ControlC => CtrlTypes.CTRL_C_EVENT,
			_ => throw new Exception("Unknown console special key")
		};

		SignalInterruptEventArgs args = new(signal, isFromKernel32: false, isTerminationImminent: false, timeoutSeconds: null);
		SignalReceived?.Invoke(this, args);

		_cancellationTokenSource.Cancel();
		eventArgs.Cancel = args.SuppressTermination;
	}

	// https://learn.microsoft.com/en-us/windows/console/handlerroutine
	/// <summary>
	/// Called by the OS via SetConsoleCtrlHandler (native handler thread).
	///
	/// For Ctrl+C and Ctrl+Break:
	/// Does NOT fire SignalReceived — these events pass through to
	/// <see cref="OnCancelKeyPress"/> (managed thread) where the subscriber
	/// can use async/await, logging, etc. Returns false to continue propagation.
	///
	/// For CTRL_CLOSE_EVENT, CTRL_LOGOFF_EVENT and CTRL_SHUTDOWN_EVENT:
	/// Fires SignalReceived on this native thread. There is no managed equivalent
	/// for these — you have ~5 seconds before the OS terminates the process.
	/// Subscribers control propagation via
	/// <see cref="SignalInterruptEventArgs.StopPropagation"/>.
	/// Return true = "handled" (stop propagation), false = "not handled".
	/// Default is false — even if you return true, OS terminates in ~5 seconds.
	/// </summary>
	private bool Kernel32Handler(CtrlTypes eventType)
	{
		switch (eventType)
		{
			case CtrlTypes.CTRL_C_EVENT:
			case CtrlTypes.CTRL_BREAK_EVENT:
				_cancellationTokenSource.Cancel();
				return false;

			case CtrlTypes.CTRL_CLOSE_EVENT:
			case CtrlTypes.CTRL_LOGOFF_EVENT:
			case CtrlTypes.CTRL_SHUTDOWN_EVENT:
			{
				SignalInterruptEventArgs args = new(eventType, isFromKernel32: true, isTerminationImminent: true, timeoutSeconds: 5);
				SignalReceived?.Invoke(this, args);
				_cancellationTokenSource.Cancel();
				return args.StopPropagation;
			}

			default:
				return false;
		}
	}
}
