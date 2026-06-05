namespace BMTP3.Core4.SignalInterrupts;

public sealed class SignalInterruptRegistrationBuilder
{
	private SignalInterruptKind _signals = SignalInterruptKind.None;
	private Action<SignalInterruptContext>? _handler = null;
	private CancellationTokenSource? _cts = null;

	public SignalInterruptRegistrationBuilder()
	{
	}

	public SignalInterruptRegistrationBuilder On(SignalInterruptKind signal)
	{
		_signals |= signal;
		return this;
	}

	public SignalInterruptRegistrationBuilder Bind(CancellationTokenSource? cts)
	{
		_cts = cts;
		return this;
	}

	public SignalInterruptRegistrationBuilder Handler(Action<SignalInterruptContext>? handler)
	{
		_handler = handler;
		return this;
	}

	public IDisposable Create()
	{
		Validate();

		Action<SignalInterruptContext> finalHandler = BuildHandler(_handler, _cts);

		return SignalInterruptEngine.Instance.Register(_signals, finalHandler);
	}

	private void Validate()
	{
		if(_signals == SignalInterruptKind.None)
		{
			throw new InvalidOperationException("No signals have been specified.");
		}

		if(_handler == null && _cts == null)
		{
			throw new InvalidOperationException("Either a handler or a CancellationTokenSource must be provided.");
		}
	}

	private Action<SignalInterruptContext> BuildHandler(Action<SignalInterruptContext>? handler, CancellationTokenSource? cts)
	{
		if(handler != null && cts != null)
		{
			return context =>
			{
				handler(context);

				if(context.RequestCancellation)
				{
					cts.Cancel();
				}
			};
		}

		if(handler != null)
		{
			return handler;
		}

		// If only cts then suppress default handling and cancel the token
		return context =>
		{
			context.SuppressDefaultHandling = true;
			cts!.Cancel();
		};
	}

}