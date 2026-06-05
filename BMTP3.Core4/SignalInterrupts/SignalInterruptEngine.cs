using System.Runtime.InteropServices;

namespace BMTP3.Core4.SignalInterrupts;


/// <summary>
/// Singleton engine that manages OS signal/interrupt subscriptions.
///
/// Registers a single <c>kernel32.SetConsoleCtrlHandler</c> callback on first
/// subscription and dispatches incoming signals to registered handlers.
///
/// Flow:
/// - OS sends a console control event → <see cref="Kernel32Handler"/> runs on a native thread.
/// - A <see cref="SignalInterruptContext"/> is created describing the event.
/// - All matching <see cref="SignalSubscription"/> instances are invoked in reverse order.
/// - Dispatch stops if <see cref="SignalInterruptContext.StopPropagation"/> is set.
/// - The result of <see cref="SignalInterruptContext.SuppressDefaultHandling"/> is returned to Windows.
///
/// Thread safety: all subscription modifications are protected by <see cref="_gate"/>.
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

	private readonly ConsoleEventDelegate _kernel32Callback;

	private bool _registered;
	private bool _disposed;

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
	///
	/// The native kernel32 callback is registered lazily on the first call,
	/// and unregistered automatically when the last subscription is disposed.
	///
	/// </summary>
	/// <param name="signals">One or more <see cref="SignalInterruptKind"/> flags.</param>
	/// <param name="handler">Callback invoked when the signal occurs.</param>
	/// <returns>
	/// An <see cref="ISignalSubscription"/> representing the registration.
	/// Dispose it to unregister the handler.
	/// </returns>
	/// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="handler"/> is null.</exception>
	/// <exception cref="ArgumentException"><paramref name="signals"/> is <see cref="SignalInterruptKind.None"/>.</exception>
	/// <exception cref="PlatformNotSupportedException">The OS is not Windows.</exception>
	/// <exception cref="InvalidOperationException">Failed to register kernel32 handler.</exception>
	public ISignalSubscription Register(
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
	/// Ensures that the native <c>SetConsoleCtrlHandler</c> callback is registered.
	///
	/// This method is called lazily when the first subscription is added.
	/// Throws if the current platform is not Windows.
	/// </summary>
	private void EnsureNativeRegistered()
	{
		if(_registered)
		{
			return;
		}

		if(!OperatingSystem.IsWindows())
		{
			throw new PlatformNotSupportedException("SignalInterrupt currently only supports Windows.");
		}

		if(!SetConsoleCtrlHandler(_kernel32Callback, true))
		{
			int error = Marshal.GetLastWin32Error();

			throw new InvalidOperationException($"SetConsoleCtrlHandler failed. Win32 error: {error}");
		}

		_registered = true;
	}

	/// <summary>
	/// Removes a subscription from all associated signal lists.
	///
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
	/// Native callback invoked by Windows when a console control event is received.
	///
	/// Creates a <see cref="SignalInterruptContext"/> from the incoming event,
	/// dispatches it to all registered handlers in reverse order, and returns the final
	/// <see cref="SignalInterruptContext.SuppressDefaultHandling"/> value.
	///
	/// Returning:
	/// - <c>true</c>  → signal handled, stop handler chain.
	/// - <c>false</c> → continue with next handler/default Windows behavior.
	///
	/// Exceptions are swallowed and treated as <c>false</c>.
	/// </summary>
	private bool Kernel32Handler(WindowsCtrlType eventType)
	{
		try
		{
			SignalInterruptContext context = CreateContext(eventType);

			Dispatch(context);

			return context.SuppressDefaultHandling;
		} catch(Exception)
		{
			// TODO: optional logging hook
			//_logger?.LogError(ex, "Unhandled exception in signal handler");
			return false;
		}

	}

	/// <summary>
	/// Dispatches a signal to all registered handlers for the given context.
	///
	/// Handlers are invoked in reverse registration order.
	/// If a handler sets <see cref="SignalInterruptContext.StopPropagation"/> to <c>true</c>,
	/// remaining handlers are not invoked.
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
	/// Creates a <see cref="SignalInterruptContext"/> from a Windows control event.
	///
	/// Mapping:
	/// - CTRL_C_EVENT      → Interrupt (not imminent, no timeout, suppress default).
	/// - CTRL_BREAK_EVENT  → Break (not imminent, no timeout, suppress default).
	/// - CTRL_CLOSE_EVENT  → ConsoleClose (imminent, ~5s timeout).
	/// - CTRL_LOGOFF_EVENT → Logoff (imminent, ~5s timeout).
	/// - CTRL_SHUTDOWN_EVENT → Shutdown (imminent, ~5s timeout).
	///
	/// For termination signals, <see cref="SignalInterruptContext.IsTerminationImminent"/> is true.
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
	/// Expands a combined <see cref="SignalInterruptKind"/> flag value into
	/// its individual signal components.
	///
	/// Used to register or unregister a subscription across multiple signal lists.
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
	/// Disposes the engine by unregistering the native handler and clearing all subscriptions.
	///
	/// After disposal, no further registrations are allowed.
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
	}

	/// <summary>
	/// Represents a single signal registration, including the subscribed signals
	/// and the associated handler.
	///
	/// Disposing this instance unregisters it from the engine.
	/// </summary>
	private sealed class SignalSubscription : ISignalSubscription
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