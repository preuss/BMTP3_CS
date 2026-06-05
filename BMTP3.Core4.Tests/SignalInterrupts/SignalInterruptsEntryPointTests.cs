using BMTP3.Core4.SignalInterrupts;

namespace BMTP3.Core4.Tests.SignalInterrupts;

public class SignalInterruptsEntryPointTests
{
	[Fact]
	public void On_ReturnsBuilder()
	{
		SignalInterruptRegistrationBuilder builder = global::SignalInterrupt.On(SignalInterruptKind.Interrupt);

		Assert.NotNull(builder);
	}

	[Fact]
	public void On_WithNone_ReturnsBuilder()
	{
		SignalInterruptRegistrationBuilder builder = global::SignalInterrupt.On(SignalInterruptKind.None);

		Assert.NotNull(builder);
	}

	[Fact]
	public void Bind_ReturnsBuilder()
	{
		using var cts = new CancellationTokenSource();

		SignalInterruptRegistrationBuilder builder = global::SignalInterrupt.Bind(cts);

		Assert.NotNull(builder);
	}

	[Fact]
	public void Create_WithSignalsAndHandler_ReturnsDisposable()
	{
		try
		{
			using IDisposable registration = global::SignalInterrupt.Create(
				SignalInterruptKind.Interrupt,
				ctx => { });

			Assert.NotNull(registration);
		} catch(PlatformNotSupportedException)
		{
			// Not running on Windows — kernel32 unavailable
		}
	}

	[Fact]
	public void Create_WithSignalsAndCts_ReturnsDisposable()
	{
		using var cts = new CancellationTokenSource();

		try
		{
			using IDisposable registration = global::SignalInterrupt.Create(
				SignalInterruptKind.Break,
				handler: null,
				cts: cts);

			Assert.NotNull(registration);
		} catch(PlatformNotSupportedException)
		{
			// Not running on Windows — kernel32 unavailable
		}
	}

	[Fact]
	public void Create_WithAllSignals_ReturnsDisposable()
	{
		try
		{
			using IDisposable registration = global::SignalInterrupt.Create(
				SignalInterruptKind.All,
				ctx => { });

			Assert.NotNull(registration);
		} catch(PlatformNotSupportedException)
		{
			// Not running on Windows — kernel32 unavailable
		}
	}

	[Fact]
	public void Create_WithoutHandlerOrCts_ThrowsInvalidOperationException()
	{
		Assert.Throws<InvalidOperationException>(() =>
			global::SignalInterrupt.Create(SignalInterruptKind.Interrupt, handler: null));
	}

	[Fact]
	public void Create_WithoutSignals_ThrowsInvalidOperationException()
	{
		using var cts = new CancellationTokenSource();

		Assert.Throws<InvalidOperationException>(() =>
			global::SignalInterrupt.Create(SignalInterruptKind.None, handler: null, cts: cts));
	}

	[Fact]
	public void Bind_DoesNotCancelCtsPrematurely()
	{
		using var cts = new CancellationTokenSource();

		Assert.False(cts.IsCancellationRequested);

		SignalInterruptRegistrationBuilder builder = global::SignalInterrupt.Bind(cts);

		Assert.NotNull(builder);
		Assert.False(cts.IsCancellationRequested,
			"Bind should not cancel the CTS — only the handler does that on signal dispatch.");
	}

	[Fact]
	public void OnBindCreate_DoesNotCancelCtsPrematurely()
	{
		using var cts = new CancellationTokenSource();

		Assert.False(cts.IsCancellationRequested);

		try
		{
			using IDisposable registration = global::SignalInterrupt
				.On(SignalInterruptKind.Interrupt)
				.Bind(cts)
				.Create();

			Assert.NotNull(registration);
			Assert.False(cts.IsCancellationRequested,
				"Create should NOT cancel the CTS — only a signal event should trigger cancellation.");
		} catch(PlatformNotSupportedException)
		{
			// Not running on Windows — kernel32 unavailable
		}
	}
}
