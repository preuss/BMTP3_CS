namespace BMTP3.Core4.Infrastructure.Throttling;

public class DelayThrottler : IThrottler
{
	private readonly int _delayMs;
	private readonly CancellationToken _token;

	public DelayThrottler(int delayMs, CancellationToken token)
	{
		if(delayMs <= 0)
			throw new ArgumentOutOfRangeException(nameof(delayMs), "Delay must be greater than zero.");

		_delayMs = delayMs;
		_token = token;
	}

	public Task WaitAsync()
	{
		return Task.Delay(_delayMs, _token);
	}
}