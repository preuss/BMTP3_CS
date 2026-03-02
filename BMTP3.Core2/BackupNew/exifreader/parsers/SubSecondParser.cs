using System.Globalization;

namespace BMTP3.Core2.BackupNew.exifreader.parsers;

public class SubSecondParser : ParserBase<long>
{
	/// <summary>
	/// Parses a sub-second string from metadata into nanoseconds.
	/// Supports up to 9 digits (nanosecond precision).
	/// Returns false if the input is null, empty, or contains invalid characters.
	/// </summary>
	/// <param name="raw">The raw sub-second string from metadata.</param>
	/// <param name="fractionalSecondsNanoseconds">The parsed value in nanoseconds.</param>
	/// <returns>True if parsing succeeded, false otherwise.</returns>
	public override bool TryParse(string? raw, out long fractionalSecondsNanoseconds)
	{
		// Initialize output
		fractionalSecondsNanoseconds = default;

		// Reject null, empty, or whitespace input
		if(string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}

		// Trim leading/trailing whitespace
		raw = raw.Trim();

		// Try parsing the raw string as a long
		// NumberStyles.None ensures only digits are allowed
		// CultureInfo.InvariantCulture ensures consistent parsing independent of locale
		if(!long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out long value))
		{
			return false;
		}

		int len = raw.Length;

		// If input has more than 9 digits, truncate to nanosecond precision
		if(len > 9)
		{
			// Determine how many least significant digits to discard
			int excess = len - 9;

			// Remove excess digits by dividing by 10 for each extra digit
			for(int i = 0; i < excess; i++)
			{
				value /= 10;
			}

			// Update length to match nanosecond precision
			len = 9;
		}

		// Scale the value to nanoseconds if it has fewer than 9 digits
		if(len < 9)
		{
			int scale = 9 - len;

			for(int i = 0; i < scale; i++)
			{
				value *= 10;
			}
		}

		// Assign normalized nanosecond value to output
		fractionalSecondsNanoseconds = value;

		return true;
	}
}