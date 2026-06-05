namespace BMTP3.Core4.SignalInterrupts;

public sealed class SignalInterruptRegistrationBuilder
{
	private readonly SignalInterruptEngine _engine;
	private SignalInterruptKind _signals;

	internal SignalInterruptRegistrationBuilder(SignalInterruptEngine engine, SignalInterruptKind signal)
	{
		_engine = engine;
		_signals = signal;
	}

	public SignalInterruptRegistrationBuilder On(SignalInterruptKind signal)
	{
		_signals |= signal;
		return this;
	}

	public IDisposable Create(Action<SignalInterruptContext> handler)
	{
		return _engine.Register(_signals, handler);
	}
}