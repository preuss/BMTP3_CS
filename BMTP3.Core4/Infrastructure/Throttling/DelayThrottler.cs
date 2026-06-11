namespace BMTP3.Core4.Infrastructure.Throttling;

public class DelayThrottler : IThrottler
{
	private readonly int _delayMs;
	private readonly CancellationToken _token;

	public DelayThrottler(int delayMs, CancellationToken token)
	{
		_delayMs = delayMs;
		_token = token;
	}

	public Task WaitAsync()
	{
		return Task.Delay(_delayMs, _token);
	}
}