using BMTP3.Core4.SignalInterrupts;

namespace BMTP3.Core4.Tests.SignalInterrupts;

public class SignalInterruptContextTests
{
	[Fact]
	public void Constructor_SetsSignal()
	{
		var ctx = new SignalInterruptContext(
			SignalInterruptKind.Interrupt, false, null, true);

		Assert.Equal(SignalInterruptKind.Interrupt, ctx.Signal);
	}

	[Fact]
	public void Constructor_SetsIsTerminationImminent()
	{
		var ctx = new SignalInterruptContext(
			SignalInterruptKind.ConsoleClose, true, 5, false);

		Assert.True(ctx.IsTerminationImminent);
	}

	[Fact]
	public void Constructor_SetsTimeoutSeconds()
	{
		var ctx = new SignalInterruptContext(
			SignalInterruptKind.ConsoleClose, true, 5, false);

		Assert.Equal(5, ctx.TimeoutSeconds);
	}

	[Fact]
	public void Constructor_TimeoutSecondsCanBeNull()
	{
		var ctx = new SignalInterruptContext(
			SignalInterruptKind.Interrupt, false, null, true);

		Assert.Null(ctx.TimeoutSeconds);
	}

	[Fact]
	public void Constructor_SetsSuppressDefaultHandling()
	{
		var ctx = new SignalInterruptContext(
			SignalInterruptKind.Interrupt, false, null, true);

		Assert.True(ctx.SuppressDefaultHandling);
	}

	[Fact]
	public void RequestCancellation_DefaultsToTrue()
	{
		var ctx = new SignalInterruptContext(
			SignalInterruptKind.Interrupt, false, null, true);

		Assert.True(ctx.RequestCancellation);
	}

	[Fact]
	public void StopPropagation_CanBeSet()
	{
		var ctx = new SignalInterruptContext(
			SignalInterruptKind.Interrupt, false, null, true);

		Assert.False(ctx.StopPropagation);

		ctx.StopPropagation = true;

		Assert.True(ctx.StopPropagation);
	}

	[Fact]
	public void SuppressDefaultHandling_CanBeSet()
	{
		var ctx = new SignalInterruptContext(
			SignalInterruptKind.Interrupt, false, null, true);

		ctx.SuppressDefaultHandling = false;

		Assert.False(ctx.SuppressDefaultHandling);
	}

	[Fact]
	public void RequestCancellation_CanBeSet()
	{
		var ctx = new SignalInterruptContext(
			SignalInterruptKind.Interrupt, false, null, true);

		ctx.RequestCancellation = false;

		Assert.False(ctx.RequestCancellation);
	}

	[Fact]
	public void ImmutableProperties_CannotBeChanged()
	{
		var ctx = new SignalInterruptContext(
			SignalInterruptKind.Break, false, null, false);

		Assert.Equal(SignalInterruptKind.Break, ctx.Signal);
		Assert.False(ctx.IsTerminationImminent);
		Assert.Null(ctx.TimeoutSeconds);
	}
}
