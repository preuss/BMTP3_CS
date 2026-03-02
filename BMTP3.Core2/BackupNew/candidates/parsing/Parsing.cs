namespace BMTP3.Core2.BackupNew.candidates.parsing;

public enum DateComponentSeparator
{
	None,             // YYYYMMDD
	Hyphen_Minus,     // YYYY-MM-DD, U+002D, 0x2D, standard ASCII hyphen-minus, ISO 8601 default
	Hyphen,           // YYYY‐MM‐DD, U+2010, 0x2010, true hyphen
	Minus_Sign,       // YYYY−MM−DD, U+2212, 0x2212, mathematical minus sign, ISO 8601 recommended
	Heavy_Minus_Sign, // YYYY➖MM➖DD, U+2796, 0x2796, heavier minus sign
	Figure_Dash,      // YYYY‒MM‒DD, U+2012, 0x2012, width of digits
	EnDash,           // YYYY–MM–DD, U+2013, 0x2013, slightly longer than Figure Dash
	EmDash,           // YYYY—MM—DD, U+2014, 0x2014, longest dash
	Dot,              // YYYY.MM.DD, U+002E, 0x2E, standard ASCII dot
	Slash,            // YYYY/MM/DD, U+002F, 0x2F, standard ASCII slash
	Space,            // YYYY MM DD, U+0020, 0x20, standard ASCII space
}
public enum TimeClockSeparator
{
	None,   // hhmmss
	Colon,  // hh:mm:ss
	Dot,    // hh.mm.ss
}
/// <summary>
/// Separator for fractional seconds or fractional duration
/// Only used when fraction exists
/// </summary>
public enum DecimalFractionSeparator
{
	Dot,   // .fff
	Comma, // ,fff
}

public enum TimeRepresentation
{
	/// <summary>
	/// Clock notation: Z or ±hh:mm[:ss[,fff]] (colon, dot, or none for compact)
	/// Seconds optional if zero, fractions require seconds
	/// </summary>
	Clock,    // Z or ±hh:mm[:ss[,fff]] or EMPTY (for Unspecified)
	/// <summary>
	/// Duration notation: Z or ±hH[mM[s[,fff]S]]
	/// Can trim trailing zero components, fractions require seconds
	/// </summary>
	Duration, // Z or ±hH[mM[s[,fff]S]] or EMPTY (for Unspecified)
}
/// <summary>
/// Defines the high-level formatting style for a clock-based time offset.
///
/// The format style expresses the intended presentation form and determines
/// which time components may be conditionally trimmed when their value is zero.
/// Trimming is non-destructive: no rounding or loss of temporal information
/// occurs.
///
/// Fractional seconds are only emitted when the seconds component is present
/// and non-zero.
/// </summary>
public enum TimeClockFormatStyle
{
	/// <summary>
	/// Full clock format.
	///
	/// Hours, minutes, and seconds are always emitted.
	/// Fractional seconds are emitted only if non-zero.
	///
	/// Format:
	/// ±hh:mm:ss[.fff]
	/// </summary>
	Full,

	/// <summary>
	/// Reduced clock format.
	///
	/// Hours and minutes are always emitted.
	/// Seconds are emitted only if non-zero.
	/// Fractional seconds are emitted only if seconds are present and non-zero.
	///
	/// Format:
	/// ±hh:mm[:ss[.fff]]
	/// </summary>
	Reduced,

	/// <summary>
	/// Basic clock format.
	///
	/// Hours are always emitted.
	/// Minutes and seconds are emitted only if non-zero.
	/// Fractional seconds are emitted only if seconds are present and non-zero.
	///
	/// Format:
	/// ±hh[:mm[:ss[.fff]]]
	/// </summary>
	Basic
}
/// <summary>
/// Defines the minimum clock unit that must be preserved after trimming
/// zero-valued components in a clock-based time offset representation.
///
/// This enum specifies a strict lower bound: trimming will never remove
/// components at or above the selected unit. It does not describe formatting
/// style, only the guaranteed minimum temporal resolution of the output.
/// </summary>
public enum TimeClockMinimumUnit
{
	/// <summary>
	/// Hours are always preserved.
	///
	/// Minutes, seconds, and fractional seconds may be omitted if zero.
	///
	/// Guaranteed format:
	/// ±hh
	/// </summary>
	Hour,

	/// <summary>
	/// Hours and minutes are always preserved.
	///
	/// Seconds and fractional seconds may be omitted if zero.
	///
	/// Guaranteed format:
	/// ±hh:mm
	/// </summary>
	Minute,

	/// <summary>
	/// Hours, minutes, and seconds are always preserved.
	///
	/// Fractional seconds may be omitted if zero.
	///
	/// Guaranteed format:
	/// ±hh:mm:ss
	/// </summary>
	Second
}


public enum DateTimeSeparatorStyle
{
	T,          // 'T', example 2024-01-01T12:00:00
	Underscore  // '_', example 2024-01-01_12:00:00
}