namespace BMTP3.Core4.Infrastructure.Throttling;

public static class ThrottlerFactory
{
	public static IThrottler Create(int delayMs, CancellationToken token)
	{
		return delayMs > 0
			? new DelayThrottler(delayMs, token)
			: new NoOpThrottler();
	}
}