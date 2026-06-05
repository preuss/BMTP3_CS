using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core4.SignalInterrupts;

public static class SignalInterrupts
{
	public static CancellationToken CancellationToken => SignalInterruptEngine.Instance.CancellationToken;

	public static SignalInterruptRegistrationBuilder On(SignalInterruptKind signal)
	{
		return new SignalInterruptRegistrationBuilder(SignalInterruptEngine.Instance, signal);
	}

	public static IDisposable Create(SignalInterruptKind signals, Action<SignalInterruptContext> handler)
	{
		return SignalInterruptEngine.Instance.Register(signals, handler);
	}
}

