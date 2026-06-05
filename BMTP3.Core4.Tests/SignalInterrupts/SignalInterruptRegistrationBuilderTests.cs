using BMTP3.Core4.SignalInterrupts;

namespace BMTP3.Core4.Tests.SignalInterrupts;

public class SignalInterruptRegistrationBuilderTests
{
	[Fact]
	public void On_Chains()
	{
		var builder = new SignalInterruptRegistrationBuilder();

		SignalInterruptRegistrationBuilder result = builder.On(SignalInterruptKind.Interrupt);

		Assert.Same(builder, result);
	}

	[Fact]
	public void On_MultipleSignals_Accumulates()
	{
		var builder = new SignalInterruptRegistrationBuilder();

		builder
			.On(SignalInterruptKind.Interrupt)
			.On(SignalInterruptKind.Break);
	}

	[Fact]
	public void On_None_DoesNotThrow()
	{
		var builder = new SignalInterruptRegistrationBuilder();

		builder.On(SignalInterruptKind.None);
	}

	[Fact]
	public void Bind_Chains()
	{
		var builder = new SignalInterruptRegistrationBuilder();
		using var cts = new CancellationTokenSource();

		SignalInterruptRegistrationBuilder result = builder.Bind(cts);

		Assert.Same(builder, result);
	}

	[Fact]
	public void Bind_Null_DoesNotThrow()
	{
		var builder = new SignalInterruptRegistrationBuilder();

		builder.Bind(null);
	}

	[Fact]
	public void Handler_Chains()
	{
		var builder = new SignalInterruptRegistrationBuilder();

		SignalInterruptRegistrationBuilder result = builder.Handler(ctx => { });

		Assert.Same(builder, result);
	}

	[Fact]
	public void Handler_Null_DoesNotThrow()
	{
		var builder = new SignalInterruptRegistrationBuilder();

		builder.Handler(null);
	}

	[Fact]
	public void Create_WithoutSignals_ThrowsInvalidOperationException()
	{
		var builder = new SignalInterruptRegistrationBuilder();

		InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
			builder.Create());

		Assert.Contains("No signals", ex.Message);
	}

	[Fact]
	public void Create_WithSignalsButNoHandlerOrCts_ThrowsInvalidOperationException()
	{
		var builder = new SignalInterruptRegistrationBuilder();
		builder.On(SignalInterruptKind.Interrupt);

		InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
			builder.Create());

		Assert.Contains("handler or a CancellationTokenSource", ex.Message);
	}

	[Fact]
	public void Create_WithHandlerOnly_ReturnsDisposable()
	{
		var builder = new SignalInterruptRegistrationBuilder();
		builder
			.On(SignalInterruptKind.Interrupt)
			.Handler(ctx => { });

		try
		{
			IDisposable registration = builder.Create();

			Assert.NotNull(registration);
			registration.Dispose();
		} catch(PlatformNotSupportedException)
		{
			// Not running on Windows — kernel32 unavailable
		}
	}

	[Fact]
	public void Create_WithCtsOnly_ReturnsDisposable()
	{
		var builder = new SignalInterruptRegistrationBuilder();
		using var cts = new CancellationTokenSource();
		builder
			.On(SignalInterruptKind.Interrupt)
			.Bind(cts);

		try
		{
			IDisposable registration = builder.Create();

			Assert.NotNull(registration);
			registration.Dispose();
		} catch(PlatformNotSupportedException)
		{
			// Not running on Windows — kernel32 unavailable
		}
	}

	[Fact]
	public void Create_WithHandlerAndCts_ReturnsDisposable()
	{
		var builder = new SignalInterruptRegistrationBuilder();
		using var cts = new CancellationTokenSource();
		builder
			.On(SignalInterruptKind.Break)
			.Handler(ctx => { })
			.Bind(cts);

		try
		{
			IDisposable registration = builder.Create();

			Assert.NotNull(registration);
			registration.Dispose();
		} catch(PlatformNotSupportedException)
		{
			// Not running on Windows — kernel32 unavailable
		}
	}

	[Fact]
	public void Create_MultipleSignals_ReturnsDisposable()
	{
		var builder = new SignalInterruptRegistrationBuilder();
		using var cts = new CancellationTokenSource();
		builder
			.On(SignalInterruptKind.Interrupt)
			.On(SignalInterruptKind.ConsoleClose)
			.On(SignalInterruptKind.Shutdown)
			.Bind(cts);

		try
		{
			IDisposable registration = builder.Create();

			Assert.NotNull(registration);
			registration.Dispose();
		} catch(PlatformNotSupportedException)
		{
			// Not running on Windows — kernel32 unavailable
		}
	}

	[Fact]
	public void Dispose_Registration_CanBeCalledMultipleTimes()
	{
		var builder = new SignalInterruptRegistrationBuilder();
		builder
			.On(SignalInterruptKind.Interrupt)
			.Handler(ctx => { });

		IDisposable registration;

		try
		{
			registration = builder.Create();
		} catch(PlatformNotSupportedException)
		{
			return; // Skip on non-Windows
		}

		registration.Dispose();
		registration.Dispose(); // Should not throw
	}
}
