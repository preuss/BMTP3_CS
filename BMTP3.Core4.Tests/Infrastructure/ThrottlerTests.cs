using System.Diagnostics;
using BMTP3.Core4.Infrastructure.Throttling;

namespace BMTP3.Core4.Tests.Infrastructure;

public class ThrottlerTests
{
	[Fact]
	public void NoOpThrottler_WaitAsync_CompletesImmediately()
	{
		IThrottler throttler = new NoOpThrottler();
		Task task = throttler.WaitAsync();
		Assert.True(task.IsCompletedSuccessfully);
	}

	[Fact]
	public async Task DelayThrottler_WaitAsync_Delays()
	{
		IThrottler throttler = new DelayThrottler(10, CancellationToken.None);
		Stopwatch sw = Stopwatch.StartNew();
		await throttler.WaitAsync();
		sw.Stop();
		Assert.True(sw.Elapsed.TotalMilliseconds >= 1,
			$"Expected positive delay, got {sw.Elapsed.TotalMilliseconds:F1}ms");
	}

	[Fact]
	public async Task DelayThrottler_CancelledToken_Throws()
	{
		using CancellationTokenSource cts = new();
		cts.Cancel();

		IThrottler throttler = new DelayThrottler(1000, cts.Token);
		await Assert.ThrowsAsync<TaskCanceledException>(() => throttler.WaitAsync());
	}

	[Fact]
	public void ThrottlerFactory_WithZeroDelay_ReturnsNoOp()
	{
		IThrottler throttler = ThrottlerFactory.Create(0, CancellationToken.None);
		Assert.IsType<NoOpThrottler>(throttler);
	}

	[Fact]
	public void ThrottlerFactory_WithPositiveDelay_ReturnsDelayThrottler()
	{
		IThrottler throttler = ThrottlerFactory.Create(50, CancellationToken.None);
		Assert.IsType<DelayThrottler>(throttler);
	}

	[Fact]
	public void ThrottlerFactory_WithNegativeDelay_ReturnsNoOp()
	{
		IThrottler throttler = ThrottlerFactory.Create(-1, CancellationToken.None);
		Assert.IsType<NoOpThrottler>(throttler);
	}
}
