namespace BMTP3.Consoles.Services;
internal class SystemClock : IClock
{
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
