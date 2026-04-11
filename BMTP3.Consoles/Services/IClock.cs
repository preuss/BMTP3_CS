namespace BMTP3.Consoles.Services;

public interface IClock
{
	DateTimeOffset UtcNow { get; }
}