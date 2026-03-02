using System;

namespace BMTP3.Core2.BackupNew.exifreader.parsers;

/// <summary>
/// Evaluates the magnitude of a numeric timestamp against its expected resolution.
/// It prevents out-of-range dates by escalating the resolution if the value magnitude
/// exceeds the limits of the expected unit.
/// </summary>
public static class TimestampResolutionEvaluator
{
	/// <summary>
	/// MaxValidTicks is the same as DateTime.MaxValue.Ticks.
	/// </summary>
	private const long MaxValidTicks = 3_155_378_976_000_000_000L - 1; // Maximum valid ticks value (0 to 3,155,378,975,999,999,999)

	private const long MilliThreshold = 1_000_000_000_000L;         // 13 digits
	private const long MicroThreshold = 1_000_000_000_000_000L;     // 16 digits
	private const long NanoThreshold = 1_000_000_000_000_000_000L; // 19 digits

	/// <summary>
	/// Defines the number of nanoseconds per unit for each timestamp resolution.
	/// Used for scaling timestamp values between different resolutions.
	/// </summary>
	private static readonly Dictionary<TimestampResolution, long> NanosecondsPerUnitMap = new Dictionary<TimestampResolution, long>()
	{
		{ TimestampResolution.Seconds, 1_000_000_000L },
		{ TimestampResolution.Milliseconds, 1_000_000L },
		{ TimestampResolution.Microseconds, 1_000L },
		{ TimestampResolution.Ticks100Ns, 100L },
		{ TimestampResolution.Nanoseconds, 1L }
	};


	public static TimestampResolution DetermineEffectiveResolution(long value, TimestampResolution expected)
	{
		long absValue = Int64.Abs(value);

		// Ticks100Ns (Windows/ .NET) has a very specific range (0 to 3,155,378,975,999,999,999).
		// If it's higher than a 19-digit magnitude, it's physically impossible for a DateTime.
		if(expected == TimestampResolution.Ticks100Ns)
		{
			// Safety margin above MaxTicks
			if(absValue > MaxValidTicks)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "The numeric value exceeds the valid range for 100-nanosecond ticks.");
			}
			return TimestampResolution.Ticks100Ns;
		}

		TimestampResolution magnitudeResolution = absValue switch
		{
			>= NanoThreshold => TimestampResolution.Nanoseconds,
			>= MicroThreshold => TimestampResolution.Microseconds,
			>= MilliThreshold => TimestampResolution.Milliseconds,
			_ => TimestampResolution.Seconds
		};

		return GetHighestPrecision(magnitudeResolution, expected);
	}

	/// <summary>
	/// Returns the resolution with highest precision (smallest time unit).
	/// Nanoseconds > Microseconds > Milliseconds > Seconds.
	/// Ticks100Ns is treated as a special case: if either resolution is Ticks100Ns, it wins.
	/// </summary>
	private static TimestampResolution GetHighestPrecision(TimestampResolution resolution1, TimestampResolution resolution2)
	{
		// Ticks100Ns is .NET-specific and not part of the standard Unix timestamp hierarchy.
		// If either resolution is Ticks100Ns, return it immediately.
		if(resolution1 == TimestampResolution.Ticks100Ns || resolution2 == TimestampResolution.Ticks100Ns)
		{
			return TimestampResolution.Ticks100Ns;
		}

		// Standard precision hierarchy
		return (resolution1, resolution2) switch
		{
			_ when resolution1 == TimestampResolution.Nanoseconds || resolution2 == TimestampResolution.Nanoseconds
				=> TimestampResolution.Nanoseconds,
			_ when resolution1 == TimestampResolution.Microseconds || resolution2 == TimestampResolution.Microseconds
				=> TimestampResolution.Microseconds,
			_ when resolution1 == TimestampResolution.Milliseconds || resolution2 == TimestampResolution.Milliseconds
				=> TimestampResolution.Milliseconds,
			_ => TimestampResolution.Seconds
		};
	}

	/// <summary>
	/// Scales a timestamp value from one resolution to another by converting through nanoseconds.
	/// </summary>
	/// <param name="value">The timestamp value to scale.</param>
	/// <param name="fromResolution">The current resolution of the value.</param>
	/// <param name="toResolution">The target resolution.</param>
	/// <returns>The scaled timestamp value in the target resolution.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if either resolution is not supported.</exception>
	/// <exception cref="OverflowException">Thrown if the conversion causes arithmetic overflow.</exception>
	public static long ScaleToResolution(long value, TimestampResolution fromResolution, TimestampResolution toResolution)
	{
		if(fromResolution == toResolution)
		{
			return value;
		}

		long fromNs = GetNanosecondsPerUnit(fromResolution);
		long toNs = GetNanosecondsPerUnit(toResolution);

		long valueInNanoseconds = checked(value * fromNs);
		return valueInNanoseconds / toNs;
	}

	/// <summary>
	/// Gets the number of nanoseconds per unit for a given timestamp resolution.
	/// </summary>
	/// <param name="resolution">The timestamp resolution.</param>
	/// <returns>The number of nanoseconds in one unit of the specified resolution.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the resolution is not supported.</exception>
	private static long GetNanosecondsPerUnit(TimestampResolution resolution)
	{
		if(NanosecondsPerUnitMap.TryGetValue(resolution, out long nanoseconds))
		{
			return nanoseconds;
		}
		throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Unsupported resolution.");
	}
}