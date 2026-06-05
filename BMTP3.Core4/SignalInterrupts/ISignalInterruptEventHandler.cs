namespace BMTP3.Core4.SignalInterrupts;

public interface ISignalInterruptEventHandler : IDisposable
{
	event EventHandler<SignalInterruptEventArgs>? SignalReceived;
	CancellationToken CancellationToken { get; }
}
