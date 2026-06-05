namespace BMTP3.Core4.SignalInterrupts;

public sealed class SignalInterruptContext
{
	public SignalInterruptKind Signal { get; }

	public bool IsTerminationImminent { get; }

	public int? TimeoutSeconds { get; }

	/// <summary>
	/// Stops invoking more handlers in our own dispatcher.
	/// This does NOT directly control Windows default handling.
	/// </summary>
	public bool StopPropagation { get; set; }

	/// <summary>
	/// Controls what the kernel32 callback returns.
	///
	/// true  => return TRUE to Windows, signal handled, stop Windows handler chain.
	/// false => return FALSE to Windows, continue to next handler/default handler.
	/// </summary>
	public bool SuppressDefaultHandling { get; set; }

	/// <summary>
	/// Controls whether the service cancellation token should be cancelled.
	/// </summary>
	public bool RequestCancellation { get; set; } = true;

	internal SignalInterruptContext(
		SignalInterruptKind signal,
		bool isTerminationImminent,
		int? timeoutSeconds,
		bool suppressDefaultHandling)
	{
		Signal = signal;
		IsTerminationImminent = isTerminationImminent;
		TimeoutSeconds = timeoutSeconds;
		SuppressDefaultHandling = suppressDefaultHandling;
	}
}