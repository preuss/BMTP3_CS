using System.Runtime.InteropServices;

namespace BMTP3.Core4.SignalInterrupts;

internal sealed class SignalInterruptEngine : IDisposable
{
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

	public CancellationToken CancellationToken => _cancellationTokenSource.Token;

	private delegate bool ConsoleEventDelegate(WindowsCtrlType eventType);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetConsoleCtrlHandler(
		ConsoleEventDelegate callback,
		bool add);

	private SignalInterruptEngine()
	{
		_kernel32Callback = Kernel32Handler;
	}

	public IDisposable Register(
		SignalInterruptKind signals,
		Action<SignalInterruptContext> handler)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		ArgumentNullException.ThrowIfNull(handler);

		if (signals == SignalInterruptKind.None)
		{
			throw new ArgumentException("At least one signal must be specified.", nameof(signals));
		}

		var subscription = new SignalSubscription(this, signals, handler);

		lock (_gate)
		{
			EnsureNativeRegistered();

			foreach (SignalInterruptKind signal in ExpandSignals(signals))
			{
				_subscriptionsBySignal[signal].Add(subscription);
			}
		}

		return subscription;
	}

	private void EnsureNativeRegistered()
	{
		if (_registered)
		{
			return;
		}

		if (!OperatingSystem.IsWindows())
		{
			throw new PlatformNotSupportedException("SignalInterrupts currently only supports Windows.");
		}

		if (!SetConsoleCtrlHandler(_kernel32Callback, true))
		{
			int error = Marshal.GetLastWin32Error();

			throw new InvalidOperationException($"SetConsoleCtrlHandler failed. Win32 error: {error}");
		}

		_registered = true;
	}

	private void Unregister(SignalSubscription subscription)
	{
		lock (_gate)
		{
			foreach (SignalInterruptKind signal in ExpandSignals(subscription.Signals))
			{
				_subscriptionsBySignal[signal].Remove(subscription);
			}

			if (!HasAnySubscriptionsUnsafe())
			{
				UnregisterNativeUnsafe();
			}
		}
	}

	private bool HasAnySubscriptionsUnsafe()
	{
		foreach (List<SignalSubscription> subscriptions in _subscriptionsBySignal.Values)
		{
			if (subscriptions.Count > 0)
			{
				return true;
			}
		}

		return false;
	}

	private void UnregisterNativeUnsafe()
	{
		if (!_registered)
		{
			return;
		}

		SetConsoleCtrlHandler(_kernel32Callback, false);
		_registered = false;
	}

	private bool Kernel32Handler(WindowsCtrlType eventType)
	{
		try
		{
			SignalInterruptContext context = CreateContext(eventType);

			Dispatch(context);

			if (context.RequestCancellation)
			{
				_cancellationTokenSource.Cancel();
			}

			return context.SuppressDefaultHandling;
		}
		catch
		{
			return false;
		}
	}

	private void Dispatch(SignalInterruptContext context)
	{
		SignalSubscription[] subscriptions;

		lock (_gate)
		{
			subscriptions = _subscriptionsBySignal[context.Signal].ToArray();
		}

		for (int i = subscriptions.Length - 1; i >= 0; i--)
		{
			SignalSubscription subscription = subscriptions[i];

			subscription.Handler(context);

			if (context.StopPropagation)
			{
				return;
			}
		}
	}

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

	private static IEnumerable<SignalInterruptKind> ExpandSignals(
		SignalInterruptKind signals)
	{
		if ((signals & SignalInterruptKind.Interrupt) != 0)
		{
			yield return SignalInterruptKind.Interrupt;
		}

		if ((signals & SignalInterruptKind.Break) != 0)
		{
			yield return SignalInterruptKind.Break;
		}

		if ((signals & SignalInterruptKind.ConsoleClose) != 0)
		{
			yield return SignalInterruptKind.ConsoleClose;
		}

		if ((signals & SignalInterruptKind.Logoff) != 0)
		{
			yield return SignalInterruptKind.Logoff;
		}

		if ((signals & SignalInterruptKind.Shutdown) != 0)
		{
			yield return SignalInterruptKind.Shutdown;
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		lock (_gate)
		{
			if (_registered)
			{
				UnregisterNativeUnsafe();
			}

			foreach (List<SignalSubscription> subscriptions in _subscriptionsBySignal.Values)
			{
				subscriptions.Clear();
			}
		}

		_cancellationTokenSource.Dispose();
	}

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

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_owner.Unregister(this);
		}
	}
}