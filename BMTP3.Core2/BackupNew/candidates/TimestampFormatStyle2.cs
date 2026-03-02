using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.candidates;
/// <summary>
/// Enum for selecting timestamp output format.
/// All formats are designed to be round-trip safe within .NET limits (max 7 fractional digits).
/// 
/// Offset rules for normal variants:
/// • Kind = Unspecified → no offset part included
/// • Kind = Utc or Offset = Zero → always 'Z'
/// • Other offsets → explicit ±hh:mm (or extended ±hh[:mm[:ss[.fffffff]]] or duration-style ±hH[mM[:s[.fffffff]S]])
/// 
/// Fractional seconds: up to 7 digits (.fffffff or ,fffffff), completely removed if zero
/// </summary>
public enum TimestampFormatStyle2
{
	// ── Standard ISO 8601 Extended (with colon, not filename safe)

	/// <summary>
	/// ISO 8601 extended with dot-decimal fractional seconds and standard clock-style offset.
	/// Format:
	///     YYYY-MM-DDThh:mm:ss[.fffffff]Z
	///     YYYY-MM-DDThh:mm:ss[.fffffff]±hh[:mm[:ss[.fffffff]]]
	/// Examples:
	///     UTC:     2026-01-13T01:41:45.1234567Z
	///     Offset:  2026-01-13T01:41:45.1234567+01:00:18.1234567
	///     Unknown: 2026-01-13T01:41:45.1234567
	/// </summary>
	Iso8601_DotFraction,

	/// <summary>
	/// Compact variant of <see cref="Iso8601_DotFraction"/>, fewer separators, same behavior.
	/// Suitable for scenarios where minimal characters are preferred.
	/// </summary>
	Iso8601_DotFraction_Compact,

	/// <summary>
	/// ISO 8601 extended with comma-decimal fractional seconds and clock-style offset.
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
	/// ISO 8601 extended with dot-decimal fractional seconds and duration-style offset.
	/// Offset expressed as ±hH[mM[:s[.fffffff]S]] (extension for detailed duration representation).
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
	/// ISO 8601 extended with comma-decimal fractional seconds and duration-style offset.
	/// Offset expressed as ±hH[mM[:s[,fffffff]S]].
	/// Format:
	///     YYYY-MM-DDThh:mm:ss[,fffffff]Z
	///     YYYY-MM-DDThh:mm:ss[,fffffff]±hH[mM[:s[,fffffff]S]]
	/// Examples:
	///     UTC:     2026-01-13T01:41:45,1234567Z
	///     Offset:  2026-01-13T01:41:45,1234567+1H30M18,1234567S
	///     Unknown: 2026-01-13T01:41:45,1234567
	/// </summary>
	Iso8601_CommaFraction_OffsetDuration,

	// ── Compact basic formats – no separators in date/time – filename friendly

	/// <summary>
	/// Compact basic ISO 8601 with dot-decimal fraction and clock-style offset.
	/// Format: YYYYMMDDThhmmss[.fffffff]Z or ±hh[mm[ss[.fffffff]]]
	/// Examples:
	///     UTC:     20260113T014145.1234567Z
	///     Offset:  20260113T014145.1234567+013018.1234567
	///     Unknown: 20260113T014145.1234567
	/// </summary>
	Compact_DotFraction,

	/// <summary>
	/// Compact basic ISO 8601 with comma-decimal fraction and clock-style offset.
	/// Format: YYYYMMDDThhmmss[,fffffff]Z or ±hh[mm[ss[,fffffff]]]
	/// </summary>
	Compact_CommaFraction,

	// ── Underscore variants – most popular for filenames

	/// <summary>
	/// Compact basic with underscore separator between date and time + dot-decimal fraction + clock-style offset.
	/// Format: YYYYMMDD_hhmmss[.fffffff]Z or ±hh[mm[ss[.fffffff]]]
	/// </summary>
	Compact_Underscore_DotFraction,

	/// <summary>
	/// Compact basic with underscore separator + comma-decimal fraction + clock-style offset.
	/// Format: YYYYMMDD_hhmmss[,fffffff]Z or ±hh[mm[ss[,fffffff]]]
	/// </summary>
	Compact_Underscore_CommaFraction,

	// ── Dot-time variants – clean look with dots in time

	/// <summary>
	/// ISO date + dots in time + dot-decimal fractional seconds + clock-style offset.
	/// Format: YYYY-MM-DDThh.mm.ss[.fffffff]Z or ±hh[.mm[.ss[.fffffff]]]
	/// </summary>
	DotTime_DotFraction,

	/// <summary>
	/// ISO date + dots in time + comma-decimal fractional seconds + clock-style offset.
	/// Format: YYYY-MM-DDThh.mm.ss[,fffffff]Z or ±hh[.mm[.ss[,fffffff]]]
	/// </summary>
	DotTime_CommaFraction,

	// ── Dot-date variants – dots in date and time

	/// <summary>
	/// Dot-date + underscore separator + dot-decimal fractional seconds + clock-style offset.
	/// Format: YYYY.MM.DD_hh.mm.ss[.fffffff]Z or ±hh[.mm[.ss[.fffffff]]]
	/// </summary>
	DotDateTime_Underscore_DotFraction,

	/// <summary>
	/// Dot-date + underscore separator + comma-decimal fractional seconds + clock-style offset.
	/// Format: YYYY.MM.DD_hh.mm.ss[,fffffff]Z or ±hh[.mm[.ss[,fffffff]]]
	/// </summary>
	DotDateTime_Underscore_CommaFraction,

	/// <summary>
	/// Dot-date + T separator + dot-decimal fractional seconds + clock-style offset.
	/// Format: YYYY.MM.DDThh.mm.ss[.fffffff]Z or ±hh[.mm[.ss[.fffffff]]]
	/// </summary>
	DotDateTime_DotFraction,

	/// <summary>
	/// Dot-date + T separator + comma-decimal fractional seconds + clock-style offset.
	/// Format: YYYY.MM.DDThh.mm.ss[,fffffff]Z or ±hh[.mm[.ss[,fffffff]]]
	/// </summary>
	DotDateTime_CommaFraction,

	// ── Danish separator variants

	/// <summary>
	/// Danish-style date separator (YYYY-MM/DD) + T separator + comma-decimal fraction + clock-style offset.
	/// Format: YYYY-MM/DDThh:mm:ss[,fffffff]Z or ±hh[:mm[:ss[,fffffff]]]
	/// </summary>
	DanishSeparator_CommaFraction,

	/// <summary>
	/// Danish-style date separator + underscore between date and time + comma-decimal fraction + clock-style offset.
	/// Format: YYYY-MM/DD_hh:mm:ss[,fffffff]Z or ±hh[:mm[:ss[,fffffff]]]
	/// </summary>
	DanishSeparator_Underscore_CommaFraction,
}
