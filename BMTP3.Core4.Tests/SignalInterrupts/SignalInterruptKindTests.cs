using BMTP3.Core4.SignalInterrupts;

namespace BMTP3.Core4.Tests.SignalInterrupts;

public class SignalInterruptKindTests
{
	[Fact]
	public void None_IsZero()
	{
		Assert.Equal(0, (int)SignalInterruptKind.None);
	}

	[Fact]
	public void Values_ArePowersOfTwo()
	{
		Assert.Equal(1, (int)SignalInterruptKind.Interrupt);
		Assert.Equal(2, (int)SignalInterruptKind.Break);
		Assert.Equal(4, (int)SignalInterruptKind.ConsoleClose);
		Assert.Equal(8, (int)SignalInterruptKind.Logoff);
		Assert.Equal(16, (int)SignalInterruptKind.Shutdown);
	}

	[Fact]
	public void All_CombinesAllSignals()
	{
		SignalInterruptKind all = SignalInterruptKind.All;

		Assert.True(all.HasFlag(SignalInterruptKind.Interrupt));
		Assert.True(all.HasFlag(SignalInterruptKind.Break));
		Assert.True(all.HasFlag(SignalInterruptKind.ConsoleClose));
		Assert.True(all.HasFlag(SignalInterruptKind.Logoff));
		Assert.True(all.HasFlag(SignalInterruptKind.Shutdown));
	}

	[Fact]
	public void HasFlag_SingleValue_Works()
	{
		SignalInterruptKind combined = SignalInterruptKind.Interrupt | SignalInterruptKind.Break;

		Assert.True(combined.HasFlag(SignalInterruptKind.Interrupt));
		Assert.True(combined.HasFlag(SignalInterruptKind.Break));
		Assert.False(combined.HasFlag(SignalInterruptKind.ConsoleClose));
		Assert.False(combined.HasFlag(SignalInterruptKind.Logoff));
	}

	[Fact]
	public void None_HasNoFlags()
	{
		Assert.False(SignalInterruptKind.None.HasFlag(SignalInterruptKind.Interrupt));
		Assert.False(SignalInterruptKind.None.HasFlag(SignalInterruptKind.All));
	}
}
