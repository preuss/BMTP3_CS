namespace BMTP3.Core4.SignalInterrupts;

/// <summary>
/// Provides context about a received OS signal/interrupt to registered handlers.
/// </summary>
public sealed class SignalInterruptContext
{
	/// <summary>
	/// The specific <see cref="SignalInterruptKind"/> that triggered this callback.
	/// Will be a single value (never a combined flags value) — the engine expands
	/// the requested signal mask and dispatches per-signal.
	/// </summary>
	public SignalInterruptKind Signal { get; }

	/// <summary>
	/// <c>true</c> when the OS will forcibly terminate the process after a short timeout
	/// (console window close, user logoff, system shutdown).
	/// <c>false</c> for Ctrl+C and Ctrl+Break, where the process can continue running.
	/// </summary>
	public bool IsTerminationImminent { get; }

	/// <summary>
	/// Estimated seconds before the OS terminates the process (typically 5).
	/// <c>null</c> for Ctrl+C and Ctrl+Break where termination is not automatic.
	/// Handlers should complete cleanup within this window.
	/// </summary>
	public int? TimeoutSeconds { get; }

	/// <summary>
	/// Stops invoking more handlers in our own dispatcher.
	/// This does NOT directly control Windows default handling.
	/// </summary>
	public bool StopPropagation { get; set; }

	/// <summary>
	/// Controls what the kernel32 callback returns to Windows.
	/// <c>true</c> => return TRUE to Windows: signal handled, stop Windows handler chain.
	/// <c>false</c> => return FALSE to Windows: continue to next handler or default handler.
	/// </summary>
	public bool SuppressDefaultHandling { get; set; }

	/// <summary>
	/// Controls whether the engine's built-in <see cref="CancellationTokenSource"/>
	/// should be cancelled after all handlers have been invoked.
	/// Default: <c>true</c>. Set to <c>false</c> to prevent automatic cancellation.
	/// </summary>
	public bool RequestCancellation { get; set; } = true;

	internal SignalInterruptContext(
		SignalInterruptKind signal,
		bool isTerminationImminent,
		int? timeoutSeconds,
		bool suppressDefaultHandling
	)
	{
		Signal = signal;
		IsTerminationImminent = isTerminationImminent;
		TimeoutSeconds = timeoutSeconds;
		SuppressDefaultHandling = suppressDefaultHandling;
	}
}