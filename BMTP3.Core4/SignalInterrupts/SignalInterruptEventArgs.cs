namespace BMTP3.Core4.SignalInterrupts;

public class SignalInterruptEventArgs : EventArgs
{
	public CtrlTypes Signal { get; }
	public bool IsTerminationImminent { get; }
	public int? TimeoutSeconds { get; }
	public bool IsFromKernel32 { get; }
	/// <summary>
	/// Controls handler chain propagation for the kernel32 mechanism.
	/// Used ONLY by <see cref="IsFromKernel32"/> = true.
	/// The FIRST subscriber that sets StopPropagation = true wins —
	/// the OS stops calling further handlers and ExitProcess is prevented.
	/// If ALL subscribers leave it false, the OS continues to the next handler
	/// in the chain, eventually calling ExitProcess.
	///
	/// CancelKeyPress (<see cref="IsFromKernel32"/> = false) ignores this property;
	/// use <see cref="SuppressTermination"/> instead.
	/// </summary>
	public bool StopPropagation { get; set; }

	/// <summary>
	/// Controls whether the application terminates after CancelKeyPress handlers run.
	/// Used ONLY by <see cref="IsFromKernel32"/> = false.
	/// ALL subscribers are called sequentially. The LAST value of SuppressTermination
	/// wins — if the final subscriber sets it to false, .NET calls ExitProcess.
	/// Default is false (allow termination).
	///
	/// kernel32 (<see cref="IsFromKernel32"/> = true) ignores this property;
	/// use <see cref="StopPropagation"/> instead.
	/// </summary>
	public bool SuppressTermination { get; set; }

	public SignalInterruptEventArgs(
		CtrlTypes signal, 
		bool isFromKernel32,
		bool isTerminationImminent, 
		int? timeoutSeconds)
	{
		Signal = signal;
		IsFromKernel32 = isFromKernel32;
		IsTerminationImminent = isTerminationImminent;
		TimeoutSeconds = timeoutSeconds;
	}
}
