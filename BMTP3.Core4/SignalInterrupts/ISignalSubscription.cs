namespace BMTP3.Core4.SignalInterrupts;
public interface ISignalSubscription : IDisposable
{
	SignalInterruptKind Signals { get; }
	Action<SignalInterruptContext> Handler { get; }
}
