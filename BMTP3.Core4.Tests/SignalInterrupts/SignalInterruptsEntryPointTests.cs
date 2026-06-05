using BMTP3.Core4.SignalInterrupts;

namespace BMTP3.Core4.Tests.SignalInterrupts;

public class SignalInterruptsEntryPointTests
{
	[Fact]
	public void On_ReturnsBuilder()
	{
		SignalInterruptRegistrationBuilder builder = global::SignalInterrupts.On(SignalInterruptKind.Interrupt);

		Assert.NotNull(builder);
	}

	[Fact]
	public void On_WithNone_ReturnsBuilder()
	{
		SignalInterruptRegistrationBuilder builder = global::SignalInterrupts.On(SignalInterruptKind.None);

		Assert.NotNull(builder);
	}

	[Fact]
	public void Bind_ReturnsBuilder()
	{
		using var cts = new CancellationTokenSource();

		SignalInterruptRegistrationBuilder builder = global::SignalInterrupts.Bind(cts);

		Assert.NotNull(builder);
	}

	[Fact]
	public void Create_WithSignalsAndHandler_ReturnsDisposable()
	{
		try
		{
			using IDisposable registration = global::SignalInterrupts.Create(
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
			using IDisposable registration = global::SignalInterrupts.Create(
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
			using IDisposable registration = global::SignalInterrupts.Create(
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
			global::SignalInterrupts.Create(SignalInterruptKind.Interrupt, handler: null));
	}

	[Fact]
	public void Create_WithoutSignals_ThrowsInvalidOperationException()
	{
		using var cts = new CancellationTokenSource();

		Assert.Throws<InvalidOperationException>(() =>
			global::SignalInterrupts.Create(SignalInterruptKind.None, handler: null, cts: cts));
	}
}
