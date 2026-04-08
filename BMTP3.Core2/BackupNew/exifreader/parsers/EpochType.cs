namespace BMTP3.Core2.BackupNew.exifreader.parsers;

/// <summary>
///     Represents different timestamp epochs and their associated units/resolutions.
///     Used when parsing raw numeric timestamp values (long) from files, databases, logs, etc.
///     Note: Some share the same epoch but differ in units (e.g. WindowsFileTime vs WebKit).
/// </summary>
public enum EpochType
{
	/// <summary>
	///     Unix Epoch (POSIX)
	/// </summary>
	/// <remarks>
	///     Start Since: 1970-01-01 00:00:00 UTC<br />
	///     Units:       Seconds<br />
	///     Usage:       Unix, Linux, macOS (modern), Android, most modern programming languages<br />
	///     Remarks:     32-bit signed version overflows on 2038-01-19 (Year 2038 problem).<br />
	///     Often represented/stored as milliseconds in APIs like JavaScript (Date.now()),<br />
	///     Java (System.currentTimeMillis()), .NET (ToUnixTimeMilliseconds()) – divide by 1000 for true seconds.
	/// </remarks>
	Unix,

	/// <summary>
	///     Mac Legacy Epoch (Classic Mac OS / HFS/HFS+)
	/// </summary>
	/// <remarks>
	///     Start Since: 1904-01-01 00:00:00 UTC<br />
	///     Units:       Seconds<br />
	///     Usage:       Classic Mac OS (pre-OS X), HFS, HFS+, QuickTime (.mov), older MP4 containers<br />
	///     Remarks:     32-bit signed; overflows around February 2040
	/// </remarks>
	MacLegacy,

	/// <summary>
	///     Cocoa / NSDate Reference Date (modern Apple)
	/// </summary>
	/// <remarks>
	///     Start Since: 2001-01-01 00:00:00 UTC<br />
	///     Units:       Seconds<br />
	///     Usage:       macOS, iOS, watchOS, tvOS (Foundation framework, NSDate, Swift Date, CFDate)
	/// </remarks>
	MacCocoa,

	/// <summary>
	///     .NET DateTime Ticks
	/// </summary>
	/// <remarks>
	///     Start Since: 0001-01-01 00:00:00 UTC<br />
	///     Units:       100-nanosecond intervals<br />
	///     Usage:       .NET (DateTime, DateTimeOffset), C#, VB.NET, F#<br />
	///     Remarks:     Range approximately year 0001 to 9999
	/// </remarks>
	DotNetTicks,

	/// <summary>
	///     Windows FILETIME / NTFS timestamp
	/// </summary>
	/// <remarks>
	///     Start Since: 1601-01-01 00:00:00 UTC<br />
	///     Units:       100-nanosecond intervals<br />
	///     Usage:       Windows NT and later, NTFS, Win32 API, .NET interop (DateTime.ToFileTime/FromFileTime)<br />
	///     Remarks:     Chosen to align with 400-year Gregorian cycle.<br />
	///     Also used as base for WebKit/Chromium/Safari internals (but in microseconds).
	/// </remarks>
	WindowsFileTime,

	/// <summary>
	///     GPS Epoch
	/// </summary>
	/// <remarks>
	///     Start Since: 1980-01-06 00:00:00 UTC<br />
	///     Units:       Seconds<br />
	///     Usage:       GPS/GNSS systems, navigation devices and software
	/// </remarks>
	GPS,

	/// <summary>
	///     NTP Timestamp
	/// </summary>
	/// <remarks>
	///     Start Since: 1900-01-01 00:00:00 UTC<br />
	///     Units:       Seconds (with fractional part)<br />
	///     Usage:       Network Time Protocol (NTP), many network time synchronization systems<br />
	///     Remarks:     32-bit seconds part rolls over in 2036
	/// </remarks>
	NTP_Timestamp,

	/// <summary>
	///     FAT / DOS Epoch
	/// </summary>
	/// <remarks>
	///     Start Since: 1980-01-01 00:00:00 (local time, often treated as UTC)<br />
	///     Units:       Seconds (2-second resolution for modification time)<br />
	///     Usage:       FAT12, FAT16, FAT32, exFAT, ZIP archives, USB drives, SD cards, legacy DOS/Windows<br />
	///     Remarks:     Supports years 1980–2107
	/// </remarks>
	FatDos
}