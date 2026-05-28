namespace BMTP3.Core4.Engine.Helpers;

internal static class TimestampHelpers
{
	private static readonly DateTimeOffset UnixEpoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

	/// <summary>
	///     Returns true if the candidate is a valid date (non-null and after the Unix epoch).
	/// </summary>
	private static bool IsValidDate(DateTimeOffset? candidate)
	{
		return candidate.HasValue && candidate.Value > UnixEpoch;
	}

	/// <summary>
	///     Returns the earliest valid (non-null, post-Unix-epoch) date from the candidates.
	/// </summary>
	/// <param name="candidates">Date candidates to evaluate.</param>
	/// <returns>
	///     The earliest <see cref="DateTimeOffset"/> that is after the Unix epoch (1970-01-01).
	/// </returns>
	/// <exception cref="ArgumentException">No valid candidates provided.</exception>
	public static DateTimeOffset FindEarliestValidDate(params DateTimeOffset?[] candidates)
	{
		List<DateTimeOffset> valid = candidates
			.Where(IsValidDate)
			.Select(candidate => candidate!.Value)
			.OrderBy(candidate => candidate)
			.ToList();

		if(valid.Count == 0)
			throw new ArgumentException("No valid date candidates provided.", nameof(candidates));

		return valid.First();
	}
}
