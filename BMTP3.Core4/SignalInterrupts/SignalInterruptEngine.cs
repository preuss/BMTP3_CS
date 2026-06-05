using System.Runtime.InteropServices;

namespace BMTP3.Core4.SignalInterrupts;

/// <summary>
/// Singleton engine that manages OS signal/interrupt subscriptions.
///
/// Registers a single <c>kernel32.SetConsoleCtrlHandler</c> callback on first
/// subscription and dispatches incoming signals to all registered handlers via
/// <see cref="Register"/>.
///
/// Flow:
/// - OS sends a console control event → <see cref="Kernel32Handler"/> runs on a
///   native thread → creates a <see cref="SignalInterruptContext"/> describing the
///   event → dispatches to all <see cref="SignalSubscription"/> instances registered
///   for that signal (reverse order, supports <see cref="SignalInterruptContext.StopPropagation"/>).
/// - After dispatch, the engine's built-in <see cref="CancellationTokenSource"/> is
///   cancelled unless a handler set <see cref="SignalInterruptContext.RequestCancellation"/>
///   to <c>false</c>.
/// - Returns <see cref="SignalInterruptContext.SuppressDefaultHandling"/> to Windows.
///
/// Thread safety: all subscription modifications are guarded by <see cref="_gate"/>.
/// See <see cref="SignalInterruptEventHandler"/> for an event-based alternative.
/// </summary>
internal sealed class SignalInterruptEngine : IDisposable
{
	/// <summary>
	/// Global singleton instance.
	/// Use this to register handlers from anywhere in the process.
	/// </summary>
	public static SignalInterruptEngine Instance { get; } = new();

	private readonly object _gate = new();

	private readonly Dictionary<SignalInterruptKind, List<SignalSubscription>> _subscriptionsBySignal = new()
	{
		[SignalInterruptKind.Interrupt] = new(),
		[SignalInterruptKind.Break] = new(),
		[SignalInterruptKind.ConsoleClose] = new(),
		[SignalInterruptKind.Logoff] = new(),
		[SignalInterruptKind.Shutdown] = new(),
	};

	private readonly CancellationTokenSource _cancellationTokenSource = new();
	private readonly ConsoleEventDelegate _kernel32Callback;

	private bool _registered;
	private bool _disposed;

	/// <summary>
	/// A <see cref="System.Threading.CancellationToken"/> that is cancelled when
	/// any OS control signal is received (unless all handlers set
	/// <see cref="SignalInterruptContext.RequestCancellation"/> to <c>false</c>).
	/// </summary>
	public CancellationToken CancellationToken => _cancellationTokenSource.Token;

	private delegate bool ConsoleEventDelegate(WindowsCtrlType eventType);

	// https://learn.microsoft.com/en-us/windows/console/setconsolectrlhandler
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

	private SignalInterruptEngine()
	{
		_kernel32Callback = Kernel32Handler;
	}

	/// <summary>
	/// Registers a handler for one or more <see cref="SignalInterruptKind"/> signals.
	/// The returned <see cref="IDisposable"/> unregisters this specific handler when disposed.
	///
	/// The native kernel32 callback is registered lazily on the first call to <see cref="Register"/>,
	/// and unregistered when the last subscription is disposed (ref-counted).
	/// </summary>
	/// <param name="signals">One or more <see cref="SignalInterruptKind"/> flags to listen for.</param>
	/// <param name="handler">Callback invoked with a <see cref="SignalInterruptContext"/> describing the event.</param>
	/// <returns>An <see cref="IDisposable"/> — dispose to unregister this handler.</returns>
	/// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
	/// <exception cref="ArgumentException"><paramref name="signals"/> is <see cref="SignalInterruptKind.None"/>.</exception>
	/// <exception cref="PlatformNotSupportedException">The current OS is not Windows.</exception>
	/// <exception cref="InvalidOperationException"><c>SetConsoleCtrlHandler</c> failed.</exception>
	public IDisposable Register(
		SignalInterruptKind signals,
		Action<SignalInterruptContext> handler
	)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		ArgumentNullException.ThrowIfNull(handler);

		if(signals == SignalInterruptKind.None)
		{
			throw new ArgumentException("At least one signal must be specified.", nameof(signals));
		}

		SignalSubscription subscription = new(this, signals, handler);

		lock(_gate)
		{
			EnsureNativeRegistered();

			foreach(SignalInterruptKind signal in ExpandSignals(signals))
			{
				_subscriptionsBySignal[signal].Add(subscription);
			}
		}

		return subscription;
	}

	/// <summary>
	/// Registers <c>SetConsoleCtrlHandler</c> if not already registered.
	/// Windows-only; throws <see cref="PlatformNotSupportedException"/> on other OS.
	/// </summary>
	private void EnsureNativeRegistered()
	{
		if(_registered)
		{
			return;
		}

		if(!OperatingSystem.IsWindows())
		{
			throw new PlatformNotSupportedException("SignalInterrupts currently only supports Windows.");
		}

		if(!SetConsoleCtrlHandler(_kernel32Callback, true))
		{
			int error = Marshal.GetLastWin32Error();

			throw new InvalidOperationException($"SetConsoleCtrlHandler failed. Win32 error: {error}");
		}

		_registered = true;
	}

	/// <summary>
	/// Removes a <see cref="SignalSubscription"/> from all signal lists.
	/// If no subscriptions remain, the native kernel32 callback is unregistered.
	/// </summary>
	private void Unregister(SignalSubscription subscription)
	{
		lock(_gate)
		{
			foreach(SignalInterruptKind signal in ExpandSignals(subscription.Signals))
			{
				_subscriptionsBySignal[signal].Remove(subscription);
			}

			if(!HasAnySubscriptionsUnsafe())
			{
				UnregisterNativeUnsafe();
			}
		}
	}

	private bool HasAnySubscriptionsUnsafe()
	{
		foreach(List<SignalSubscription> subscriptions in _subscriptionsBySignal.Values)
		{
			if(subscriptions.Count > 0)
			{
				return true;
			}
		}

		return false;
	}

	private void UnregisterNativeUnsafe()
	{
		if(!_registered)
		{
			return;
		}

		SetConsoleCtrlHandler(_kernel32Callback, false);
		_registered = false;
	}

	// https://learn.microsoft.com/en-us/windows/console/handlerroutine
	/// <summary>
	/// Called by the OS on a native handler thread when a console control event is received.
	///
	/// Creates a <see cref="SignalInterruptContext"/> via <see cref="CreateContext"/>,
	/// dispatches to all registered handlers via <see cref="Dispatch"/>, then cancels
	/// the engine's <see cref="CancellationTokenSource"/> unless a handler set
	/// <see cref="SignalInterruptContext.RequestCancellation"/> to <c>false</c>.
	///
	/// Returns <see cref="SignalInterruptContext.SuppressDefaultHandling"/> to Windows:
	/// <c>true</c> = signal handled, stop handler chain; <c>false</c> = continue to next handler.
	/// On exception, returns <c>false</c> (let Windows handle it).
	/// </summary>
	private bool Kernel32Handler(WindowsCtrlType eventType)
	{
		try
		{
			SignalInterruptContext context = CreateContext(eventType);

			Dispatch(context);

			if(context.RequestCancellation)
			{
				_cancellationTokenSource.Cancel();
			}

			return context.SuppressDefaultHandling;
		} catch
		{
			return false;
		}
	}

	/// <summary>
	/// Invokes all registered <see cref="SignalSubscription"/> handlers for the
	/// signal specified in <paramref name="context"/>.
	///
	/// Handlers are called in reverse registration order.
	/// If any handler sets <see cref="SignalInterruptContext.StopPropagation"/> to <c>true</c>,
	/// remaining handlers are skipped.
	/// </summary>
	private void Dispatch(SignalInterruptContext context)
	{
		SignalSubscription[] subscriptions;

		lock(_gate)
		{
			subscriptions = _subscriptionsBySignal[context.Signal].ToArray();
		}

		for(int i = subscriptions.Length - 1; i >= 0; i--)
		{
			SignalSubscription subscription = subscriptions[i];

			subscription.Handler(context);

			if(context.StopPropagation)
			{
				return;
			}
		}
	}

	/// <summary>
	/// Creates a <see cref="SignalInterruptContext"/> from a <see cref="WindowsCtrlType"/>.
	///
	/// Maps:
	/// - <see cref="WindowsCtrlType.CTRL_C_EVENT"/> → <see cref="SignalInterruptKind.Interrupt"/>
	///   (no timeout, not imminent, suppress default).
	/// - <see cref="WindowsCtrlType.CTRL_BREAK_EVENT"/> → <see cref="SignalInterruptKind.Break"/>
	///   (no timeout, not imminent, suppress default).
	/// - <see cref="WindowsCtrlType.CTRL_CLOSE_EVENT"/> → <see cref="SignalInterruptKind.ConsoleClose"/>
	///   (5s timeout, imminent, no suppress).
	/// - <see cref="WindowsCtrlType.CTRL_LOGOFF_EVENT"/> → <see cref="SignalInterruptKind.Logoff"/>
	///   (5s timeout, imminent, no suppress).
	/// - <see cref="WindowsCtrlType.CTRL_SHUTDOWN_EVENT"/> → <see cref="SignalInterruptKind.Shutdown"/>
	///   (5s timeout, imminent, no suppress).
	/// </summary>
	private static SignalInterruptContext CreateContext(WindowsCtrlType eventType)
	{
		return eventType switch
		{
			WindowsCtrlType.CTRL_C_EVENT =>
				new SignalInterruptContext(
					SignalInterruptKind.Interrupt,
					isTerminationImminent: false,
					timeoutSeconds: null,
					suppressDefaultHandling: true),

			WindowsCtrlType.CTRL_BREAK_EVENT =>
				new SignalInterruptContext(
					SignalInterruptKind.Break,
					isTerminationImminent: false,
					timeoutSeconds: null,
					suppressDefaultHandling: true),

			WindowsCtrlType.CTRL_CLOSE_EVENT =>
				new SignalInterruptContext(
					SignalInterruptKind.ConsoleClose,
					isTerminationImminent: true,
					timeoutSeconds: 5,
					suppressDefaultHandling: false),

			WindowsCtrlType.CTRL_LOGOFF_EVENT =>
				new SignalInterruptContext(
					SignalInterruptKind.Logoff,
					isTerminationImminent: true,
					timeoutSeconds: 5,
					suppressDefaultHandling: false),

			WindowsCtrlType.CTRL_SHUTDOWN_EVENT =>
				new SignalInterruptContext(
					SignalInterruptKind.Shutdown,
					isTerminationImminent: true,
					timeoutSeconds: 5,
					suppressDefaultHandling: false),

			_ => throw new ArgumentOutOfRangeException(
				nameof(eventType),
				eventType,
				"Unknown Windows console control event.")
		};
	}

	/// <summary>
	/// Expands a combined <see cref="SignalInterruptKind"/> flags value into individual
	/// signal values. Used to register/unregister a subscription across all relevant
	/// signal lists.
	/// </summary>
	private static IEnumerable<SignalInterruptKind> ExpandSignals(SignalInterruptKind signals)
	{
		if((signals & SignalInterruptKind.Interrupt) != 0)
		{
			yield return SignalInterruptKind.Interrupt;
		}

		if((signals & SignalInterruptKind.Break) != 0)
		{
			yield return SignalInterruptKind.Break;
		}

		if((signals & SignalInterruptKind.ConsoleClose) != 0)
		{
			yield return SignalInterruptKind.ConsoleClose;
		}

		if((signals & SignalInterruptKind.Logoff) != 0)
		{
			yield return SignalInterruptKind.Logoff;
		}

		if((signals & SignalInterruptKind.Shutdown) != 0)
		{
			yield return SignalInterruptKind.Shutdown;
		}
	}

	/// <summary>
	/// Disposes the engine: unregisters the native callback and clears all subscriptions.
	/// After disposal, the engine cannot be used for new registrations.
	/// </summary>
	public void Dispose()
	{
		if(_disposed)
		{
			return;
		}

		_disposed = true;

		lock(_gate)
		{
			if(_registered)
			{
				UnregisterNativeUnsafe();
			}

			foreach(List<SignalSubscription> subscriptions in _subscriptionsBySignal.Values)
			{
				subscriptions.Clear();
			}
		}

		_cancellationTokenSource.Dispose();
	}

	/// <summary>
	/// Represents a single registration — a combination of <see cref="SignalInterruptKind"/>
	/// flags and an <see cref="Action{T}"/> callback.
	/// Disposing the subscription unregisters it from the engine.
	/// </summary>
	private sealed class SignalSubscription : IDisposable
	{
		private readonly SignalInterruptEngine _owner;
		private bool _disposed;

		public SignalInterruptKind Signals { get; }

		public Action<SignalInterruptContext> Handler { get; }

		public SignalSubscription(
			SignalInterruptEngine owner,
			SignalInterruptKind signals,
			Action<SignalInterruptContext> handler
		)
		{
			_owner = owner;
			Signals = signals;
			Handler = handler;
		}

		/// <summary>
		/// Unregisters this subscription from the engine.
		/// Safe to call multiple times.
		/// </summary>
		public void Dispose()
		{
			if(_disposed)
			{
				return;
			}

			_disposed = true;
			_owner.Unregister(this);
		}
	}
}