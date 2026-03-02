namespace BMTP3.Core2.BackupNew.exifreader.parsers;
/// <summary>
/// Defines the time resolution / unit of a numeric timestamp value.
/// </summary>

public enum TimestampResolution
{
	/// <summary>
	/// Seconds (1 unit = 1 second)
	/// </summary>
	Seconds,

	/// <summary>
	/// Milliseconds (1 unit = 1 ms)
	/// </summary>
	Milliseconds,

	/// <summary>
	/// Microseconds (1 unit = 1 µs)
	/// </summary>
	Microseconds,

	/// <summary>
	/// 100-nanosecond ticks (1 unit = 100 ns) – used by Windows FILETIME and .NET DateTime.Ticks
	/// </summary>
	Ticks100Ns,

	/// <summary>
	/// Nanoseconds (1 unit = 1 ns)	
	/// </summary>
	Nanoseconds,
}
