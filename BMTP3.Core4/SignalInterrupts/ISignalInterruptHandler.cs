namespace BMTP3.Core4.SignalInterrupts;

public interface ISignalInterruptHandler
{
	SignalInterruptKind Signals { get; }

	void Handle(SignalInterruptContext context);
}