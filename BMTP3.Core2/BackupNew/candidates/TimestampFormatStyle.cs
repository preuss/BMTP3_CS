using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.candidates;
/// <summary>
/// Enum for selecting timestamp output format.
/// All formats will normally only be able to round-trip safe within .NET limits (max 7 fraction digits).
/// 
/// Offset rules for normal variants:
/// • Kind = Unspecified → no offset part at all
/// • Kind = Utc or Offset = Zero → always 'Z'
/// • Other offset → explicit ±hh:mm (or more detailed in extensions)
/// 
/// Fractional seconds: up to 7 digits (.fffffff or ,fffffff) – removed completely if zero
/// </summary>
public enum TimestampFormatStyle
{
	// ── Standard ISO 8601 Extended (with colon) – not Windows filename safe

	/// <summary>
	/// ISO 8601 extended with dot-decimal fraction, clock-style offset
	/// Format:
	///     YYYY-MM-DDThh:mm:ss[.fffffff]Z
	///     YYYY-MM-DDThh:mm:ss[.fffffff]±hh[:mm[:ss[.fffffff]]]
	///     YYYY-MM-DDThh:mm:ss[.fffffff]Z
	/// Examples:
	///     UTC:     2026-01-13T01:41:45.1234567Z
	///     Offset:  2026-01-13T01:41:45.1234567+01:00:18.1234567
	///     Unknown: 2026-01-13T01:41:45.1234567
	/// </summary>
	Iso8601_DotFraction,

	/// <summary>
	/// Compact variant of Iso8601_DotFraction (same as above, less separators)
	/// </summary>
	Iso8601_DotFraction_Compact,

	/// <summary>
	/// ISO 8601 extended with comma-decimal fraction, clock-style offset
	/// Format:
	///     YYYY-MM-DDThh:mm:ss[,fffffff]Z
	///     YYYY-MM-DDThh:mm:ss[,fffffff]±hh[:mm[:ss[,fffffff]]]
	/// Examples:
	///     UTC:     2026-01-13T01:41:45,1234567Z
	///     Offset:  2026-01-13T01:41:45,1234567+01:00:18,1234567
	///     Unknown: 2026-01-13T01:41:45,1234567
	/// </summary>
	Iso8601_CommaFraction,

	/// <summary>
	/// ISO 8601 extended with dot-decimal fraction, duration-style offset (extension)
	/// Format:
	///     YYYY-MM-DDThh:mm:ss[.fffffff]Z
	///     YYYY-MM-DDThh:mm:ss[.fffffff]±hH[mM[:s[.fffffff]S]]
	/// Examples:
	///     UTC:     2026-01-13T01:41:45.1234567Z
	///     Offset:  2026-01-13T01:41:45.1234567+1H30M18.1234567S
	///     Unknown: 2026-01-13T01:41:45.1234567
	/// </summary>
	Iso8601_DotFraction_OffsetDuration,

	/// <summary>
	/// ISO extended with comma-decimal fraction, duration-style offset (extension)
	/// Format:
	///     YYYY-MM-DDThh:mm:ss[,fffffff]Z
	///     YYYY-MM-DDThh:mm:ss[,fffffff]±hH[mM[:s[,fffffff]S]]
	/// Examples:
	///     UTC:     2026-01-13T01:41:45,1234567Z
	///     Offset:  2026-01-13T01:41:45,1234567+1H30M18,1234567S
	///     Unknown: 2026-01-13T01:41:45,1234567
	/// </summary>
	Iso8601_CommaFraction_OffsetDuration,

	// ── Compact basic formats – no separators in date/time – excellent for filenames

	/// <summary>
	/// Compact basic ISO 8601 with dot-decimal fraction, clock-style offset
	/// Format:
	///     YYYYMMDDThhmmss[.fffffff]Z
	///     YYYYMMDDThhmmss[.fffffff]±hh[mm[ss[.fffffff]]]
	/// Examples:
	///     UTC:     20260113T014145.1234567Z
	///     Offset:  20260113T014145.1234567+013018.1234567
	///     Unknown: 20260113T014145.1234567
	/// </summary>
	Compact_DotFraction,

	/// <summary>
	/// Compact basic ISO 8601 with comma-decimal fraction, clock-style offset
	/// Format:
	///     YYYYMMDDThhmmss[,fffffff]Z
	///     YYYYMMDDThhmmss[,fffffff]±hh[mm[ss[,fffffff]]]
	/// Examples:
	///     UTC:     20260113T014145,1234567Z
	///     Offset:  20260113T014145,1234567+013018,1234567
	///     Unknown: 20260113T014145,1234567
	/// </summary>
	Compact_CommaFraction,

	// ── Underscore variants – most popular for filenames

	/// <summary>
	/// Compact basic with Underscore date and time separator + dot-decimal + clock-style offset
	/// Format:
	///     YYYYMMDD_hhmmss[.fffffff]Z
	///     YYYYMMDD_hhmmss[.fffffff]±hh[mm[ss[.fffffff]]]
	/// Examples:
	///     UTC:     20260113_014145.1234567Z
	///     Offset:  20260113_014145.1234567+013018.1234567
	///     Unknown: 20260113_014145.1234567
	/// </summary>
	Compact_Underscore_DotFraction,

	/// <summary>
	/// Compact basic with Underscore date and time separator + comma-decimal + clock-style offset
	/// Format:
	///     YYYYMMDD_hhmmss[,fffffff]Z
	///     YYYYMMDD_hhmmss[,fffffff]±hh[mm[ss[,fffffff]]]
	/// Examples:
	///     UTC:     20260113_014145,1234567Z
	///     Offset:  20260113_014145,1234567+013018,1234567
	///     Unknown: 20260113_014145,1234567
	/// </summary>
	Compact_Underscore_CommaFraction,

	// ── Dot-time variants – clean look with dots in time

	/// <summary>
	/// ISO date + dots in time + comma-decimal + normal offset logic
	/// Format:
	///     YYYY-MM-DDThh.mm.ss[.fffffff]Z
	///     YYYY-MM-DDThh.mm.ss[.fffffff]±hh[.mm[.ss[.fffffff]]]
	/// Examples:
	///     UTC:     2026-01-13T01.41.45.1234567Z
	///     Offset:  2026-01-13T01.41.45.1234567+01.00.18.1234567
	///     Unknown: 2026-01-13T01.41.45.1234567
	/// </summary>
	DotTime_DotFraction,

	/// <summary>
	/// ISO date + dots in time + comma-decimal + normal offset logic
	/// Format:
	///     YYYY-MM-DDThh.mm.ss[,fffffff]Z
	///     YYYY-MM-DDThh.mm.ss[,fffffff]±hh[.mm[.ss[,fffffff]]]
	/// Examples:
	///     UTC:     2026-01-13T01.41.45,1234567Z
	///     Offset:  2026-01-13T01.41.45,1234567+01.00.18,1234567
	///     Unknown: 2026-01-13T01.41.45,1234567
	/// </summary>
	DotTime_CommaFraction,

	// ── Dot-date variants – dots also in date part

	/// <summary>
	/// Dot-date + underscore + dot-decimal + normal offset logic
	/// Format:
	///     YYYY.MM.DD_hh.mm.ss[.fffffff]Z
	///     YYYY.MM.DD_hh.mm.ss[.fffffff]±hh[.mm[.ss[.fffffff]]]
	/// Examples:
	///     UTC:     2026.01.13_01.41.45.1234567Z
	///     Offset:  2026.01.13_01.41.45.1234567+01.00.18.1234567
	///     Unknown: 2026.01.13_01.41.45.1234567
	/// </summary>
	DotDateTime_Underscore_DotFraction,

	/// <summary>
	/// Dot-date + underscore + comma-decimal + normal offset logic
	/// Format:
	///     YYYY.MM.DD_hh.mm.ss[,fffffff]Z
	///     YYYY.MM.DD_hh.mm.ss[,fffffff]±hh[.mm[.ss[,fffffff]]]
	/// Examples:
	///     UTC:     2026.01.13_01.41.45,1234567Z
	///     Offset:  2026.01.13_01.41.45,1234567+01.00.18,1234567
	///     Unknown: 2026.01.13_01.41.45,1234567
	/// </summary>
	DotDateTime_Underscore_CommaFraction,

	/// <summary>
	/// Dot-date + T + dot-decimal + normal offset logic
	/// Format:
	///     YYYY.MM.DDThh.mm.ss[.fffffff]Z
	///     YYYY.MM.DDThh.mm.ss[.fffffff]±hh[.mm[.ss[.fffffff]]]
	/// Examples:
	///     UTC:     2026.01.13T01.41.45.1234567Z
	///     Offset:  2026.01.13T01.41.45.1234567+01.00.18.1234567
	///     Unknown: 2026.01.13T01.41.45.1234567
	/// </summary>
	DotDateTime_DotFraction,

	/// <summary>
	/// Dot-date + T + comma-decimal + normal offset logic
	/// Format:
	///     YYYY.MM.DDThh.mm.ss[,fffffff]Z
	///     YYYY.MM.DDThh.mm.ss[,fffffff]±hh[.mm[.ss[,fffffff]]]
	/// Examples:
	///     UTC:     2026.01.13T01.41.45,1234567Z
	///     Offset:  2026.01.13T01.41.45,1234567+01.00.18,1234567
	///     Unknown: 2026.01.13T01.41.45,1234567
	/// </summary>
	DotDateTime_CommaFraction,
	// ── Danish separator variants

	/// <summary>
	/// Danish date separator (YYYY-MM/DD) + T + comma-decimal + normal offset logic
	/// Format:
	///     YYYY-MM/DDThh:mm:ss[,fffffff]Z
	///     YYYY-MM/DDThh:mm:ss[,fffffff]±hh[:mm[:ss[,fffffff]]]
	/// Examples:
	///     UTC:     2026-01/13T01:41:45,1234567Z
	///     Offset:  2026-01/13T01:41:45,1234567+01:00:18,1234567
	///     Unknown: 2026-01/13T01:41:45,1234567
	/// </summary>
	DanishSeparator_CommaFraction,

	/// <summary>
	/// Danish date separator (YYYY-MM/DD) + underscore + comma-decimal + normal offset logic
	/// Format:
	///     YYYY-MM/DD_hh:mm:ss[,fffffff]Z
	///     YYYY-MM/DD_hh:mm:ss[,fffffff]±hh[:mm[:ss[,fffffff]]]
	/// Examples:
	///     UTC:     2026-01/13_01:41:45,1234567Z
	///     Offset:  2026-01/13_01:41:45,1234567+01:00:18,1234567
	///     Unknown: 2026-01/13_01:41:45,1234567
	/// </summary>
	DanishSeparator_Underscore_CommaFraction,
}