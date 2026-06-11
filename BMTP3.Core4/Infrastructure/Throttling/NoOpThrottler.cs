namespace BMTP3.Core4.Infrastructure.Throttling;

public class NoOpThrottler : IThrottler
{
	public Task WaitAsync() => Task.CompletedTask;
}
