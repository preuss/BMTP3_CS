namespace BMTP3.Core4.Engine.Helpers;

internal static class TimestampHelpers
{
	private static readonly DateTimeOffset UnixEpoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

	/// <summary>
	///     Returns the earliest valid date from the candidates, with <see cref="DateTimeOffset.Now" /> as
	///     fallback (Core-mønster). Dates at or before Unix epoch (1970-01-01) are considered invalid
	///     placeholders and are skipped.
	/// </summary>
	public static DateTimeOffset FindEarliestValidDate(params DateTimeOffset?[] candidates)
	{
		DateTimeOffset earliest = DateTimeOffset.Now;

		foreach(DateTimeOffset? candidate in candidates)
		{
			if(!candidate.HasValue)
				continue;

			if(candidate.Value <= UnixEpoch)
				continue;

			if(candidate.Value < earliest)
				earliest = candidate.Value;
		}

		return earliest;
	}
}
