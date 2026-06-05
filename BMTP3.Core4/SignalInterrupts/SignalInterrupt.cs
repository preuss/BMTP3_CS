using BMTP3.Core4.SignalInterrupts;

/// <summary>
/// Entry point for registering OS signal/interrupt handlers.
/// </summary>
/// <example>
/// <code>
/// // Cancel a CancellationTokenSource on Ctrl+C:
/// using var _ = SignalInterrupt.On(SignalInterruptKind.Interrupt).Bind(cts).Create();
///
/// // Custom handler with CTS cancel on Ctrl+C or Ctrl+Break:
/// using var _ = SignalInterrupt.Create(
///     SignalInterruptKind.Interrupt | SignalInterruptKind.Break,
///     ctx => Console.WriteLine("Interrupt received"),
///     cts);
/// </code>
/// </example>
public static class SignalInterrupt
{
	/// <summary>
	/// Begins a fluent registration for the specified <paramref name="signal"/>.
	/// Chain with <see cref="SignalInterruptRegistrationBuilder.Bind"/> and/or
	/// <see cref="SignalInterruptRegistrationBuilder.Handler"/>, then call
	/// <see cref="SignalInterruptRegistrationBuilder.Create"/>.
	/// </summary>
	/// <param name="signal">The <see cref="SignalInterruptKind"/> to handle (supports flags combination).</param>
	/// <returns>A <see cref="SignalInterruptRegistrationBuilder"/> for further configuration.</returns>
	public static SignalInterruptRegistrationBuilder On(SignalInterruptKind signal)
	{
		return new SignalInterruptRegistrationBuilder()
			.On(signal);
	}

	/// <summary>
	/// Begins a fluent registration that cancels the specified <see cref="CancellationTokenSource"/>
	/// when a signal is received.
	/// Chain with <see cref="SignalInterruptRegistrationBuilder.On"/> and/or
	/// <see cref="SignalInterruptRegistrationBuilder.Handler"/>, then call
	/// <see cref="SignalInterruptRegistrationBuilder.Create"/>.
	/// </summary>
	/// <param name="cts">The <see cref="CancellationTokenSource"/> to cancel on signal.</param>
	/// <returns>A <see cref="SignalInterruptRegistrationBuilder"/> for further configuration.</returns>
	public static SignalInterruptRegistrationBuilder Bind(CancellationTokenSource cts)
	{
		return new SignalInterruptRegistrationBuilder()
			.Bind(cts);
	}

	/// <summary>
	/// Creates a registration that invokes <paramref name="handler"/> and/or cancels
	/// <paramref name="cts"/> when any of the specified <paramref name="signals"/> is received.
	/// Dispose the returned <see cref="IDisposable"/> to unregister.
	/// </summary>
	/// <param name="signals">One or more <see cref="SignalInterruptKind"/> flags to listen for.</param>
	/// <param name="handler">Optional callback invoked with a <see cref="SignalInterruptContext"/> describing the event.</param>
	/// <param name="cts">Optional <see cref="CancellationTokenSource"/> to cancel on signal.</param>
	/// <returns>An <see cref="IDisposable"/> — dispose to unregister the handler.</returns>
	public static IDisposable Create(SignalInterruptKind signals, Action<SignalInterruptContext>? handler, CancellationTokenSource? cts = null)
	{
		return new SignalInterruptRegistrationBuilder()
			.On(signals)
			.Bind(cts)
			.Handler(handler)
			.Create();
	}
}