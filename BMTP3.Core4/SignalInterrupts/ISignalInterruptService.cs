namespace BMTP3.Core4.SignalInterrupts;

public interface ISignalInterruptService : IDisposable
{
	CancellationToken CancellationToken { get; }

	/// <summary>
	/// Registers the native OS callback once.
	/// On Windows this calls SetConsoleCtrlHandler.
	/// </summary>
	void Register();

	/// <summary>
	/// Registers a dynamic handler for one or more signals.
	/// Dispose the returned object to unregister this specific handler.
	/// </summary>
	IDisposable Register(
		SignalInterruptKind signals,
		Action<SignalInterruptContext> handler);

	/// <summary>
	/// Fluent registration.
	/// Example:
	/// using var r = signals.On(Interrupt).On(Break).Run(ctx => ...);
	/// </summary>
	SignalInterruptRegistrationBuilder On(SignalInterruptKind signal);
}