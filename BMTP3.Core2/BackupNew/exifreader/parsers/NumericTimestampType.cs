namespace BMTP3.Core2.BackupNew.exifreader.parsers;

public enum NumericTimestampType
{
	// Seconds since 1970-01-01 00:00:00 UTC
	UnixTimeSeconds,
	// Milliseconds since 1970-01-01 00:00:00 UTC
	UnixTimeMilliseconds,
	// Microseconds since 1970-01-01 00:00:00 UTC
	UnixTimeMicroseconds,
	// Nanoseconds since 1970-01-01 00:00:00 UTC
	UnixTimeNanoseconds,
	// .NET ticks: 100-nanosecond intervals since 0001-01-01 00:00:00 UTC
	DotNetTimeTicks,
	// Windows FILETIME: 100-nanosecond intervals since 1601-01-01 00:00:00 UTC, relevant for NTFS and some Windows APIs (1601 epoch)
	WindowsFileTimeTicks,
	// Seconds since 1904-01-01 00:00:00 UTC, legacy Mac epoch, relevant for QuickTime/MP4/MOV (1904 epoch)
	MacTimeSeconds,
	// Seconds since 2001-01-01 00:00:00 UTC, current Apple-specific epoch
	CocoaTimeSeconds,
}
