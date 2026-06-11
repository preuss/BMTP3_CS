namespace BMTP3.Core4.Infrastructure.Throttling;

public interface IThrottler
{
	Task WaitAsync();
}
