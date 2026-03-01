using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BMTP3.Consoles.exifreader.parsers;

public class TimestampParser : ParserBase<long>
{
	/// <summary>
	/// Parses a numeric epoch/timestamp token (integer) into a long.
	/// Accepts optional leading/trailing whitespace and an optional leading '+' or '-' sign.
	/// Returns false for null/empty input or non-integer values (e.g. strings with decimal separators).
	/// </summary>
	public override bool TryParse(string? raw, [NotNullWhen(true)] out long timestamp)
	{
		timestamp = default;

		if(string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}

		raw = raw.Trim();

		// Only accept integer-style numeric representations for epoch values.
		// This keeps the parser simple and unambiguous: it returns the raw numeric value
		// as a long. Detection of resolution/epoch (seconds, milliseconds, ticks, etc.)
		// should be performed by higher-level logic (TimestampResolutionEvaluator / factories).
		return long.TryParse(
			raw,
			NumberStyles.Integer,
			CultureInfo.InvariantCulture,
			out timestamp
		);
	}
}
