using BMTP3.Consoles.candidates.parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.candidates;
public sealed record TimestampSources
{
	public string? Date { get; init; }          // e.g. "2000:01:01"
	public string? Time { get; init; }          // e.g. "12:53:02"
	public string? SubSeconds { get; init; }    // e.g. "456"
	public string? Offset { get; init; }        // e.g. "+02:00"
	public string? DateTime { get; init; }      // e.g. "2000:01:01 12:53:02"
	public string? DateTimeOffset { get; init; }// e.g. "2000:01:01 12:53:02+02:00"

	public string? Timestamp { get; init; }     // e.g. "1647416932752112"

	public static readonly TimestampSources Empty = new();

	public TimestampSources()
	{
	}
	public TimestampSources(
		string? date,
		string? time,
		string? subSeconds,
		string? offset,
		string? dateTime,
		string? dateTimeOffset,
		string? timestamp
	)
	{
		Date = date;
		Time = time;
		SubSeconds = subSeconds;
		Offset = offset;
		DateTime = dateTime;
		DateTimeOffset = dateTimeOffset;
		Timestamp = timestamp;
	}

	public bool IsEmpty =>
		string.IsNullOrWhiteSpace(Date) &&
		string.IsNullOrWhiteSpace(Time) &&
		string.IsNullOrWhiteSpace(SubSeconds) &&
		string.IsNullOrWhiteSpace(Offset) &&
		string.IsNullOrWhiteSpace(DateTime) &&
		string.IsNullOrWhiteSpace(DateTimeOffset) &&
		string.IsNullOrWhiteSpace(Timestamp);

	public bool IsAllNull =>
		Date is null &&
		Time is null &&
		SubSeconds is null &&
		Offset is null &&
		DateTime is null &&
		DateTimeOffset is null &&
		Timestamp is null;

	public override string ToString()
	{
		if(IsAllNull) return "<Sources/>";

		StringBuilder builder = new();
		builder.Append("<Sources>");

		if(Date != null) builder.Append($"<Date>{Date}</Date>");
		if(Time != null) builder.Append($"<Time>{Time}</Time>");
		if(SubSeconds != null) builder.Append($"<SubSeconds>{SubSeconds}</SubSeconds>");
		if(Offset != null) builder.Append($"<Offset>{Offset}</Offset>");
		if(DateTime != null) builder.Append($"<DateTime>{DateTime}</DateTime>");
		if(DateTimeOffset != null) builder.Append($"<DateTimeOffset>{DateTimeOffset}</DateTimeOffset>");
		if(Timestamp != null) builder.Append($"<Timestamp>{Timestamp}</Timestamp>");

		builder.Append("</Sources>");

		return builder.ToString();
	}

	public string ToDebugString()
	{
		return
			$"TimestampSources(" +
			$"Date={Date ?? "∅"}, " +
			$", Time={Time ?? "∅"}, " +
			$", SubSeconds={SubSeconds ?? "∅"}, " +
			$", Offset={Offset ?? "∅"}, " +
			$", DateTime={DateTime ?? "∅"}, " +
			$", DateTimeOffset={DateTimeOffset ?? "∅"}" +
			$", Timestamp={Timestamp ?? "∅"}" +
			")";
	}

	/// <summary>

	/// </summary>
	/// <returns>
	/// The current <see cref="TimestampSources"/> instance when at least one timestamp-related
	/// property contains non-empty, non-whitespace text; otherwise <see cref="TimestampSources.Empty"/>.
	/// </returns>
	/// <remarks>
	/// This instance method delegates to <see cref="Normalize(TimestampSources?)"/>.
	/// The normalization treats null, empty, and whitespace-only strings as absent.
	/// </remarks>
	public TimestampSources Normalize()
	{
		return Normalize(this);
	}

	/// <summary>
	/// Returns a normalized version of this instance.
	/// Return <see cref="TimestampSources.Empty"/> when the provided sources argument is null
	/// or all its fields are null/empty/whitespace. Otherwise return the original instance.
	/// </summary>
	/// <param name="sources">The instance to normalize. May be <c>null</c>.</param>
	/// <returns>
	/// <see cref="TimestampSources.Empty"/> when <paramref name="sources"/> is <c>null</c> or all
	/// timestamp-related properties are null, empty, or consist only of whitespace; otherwise
	/// returns the original <paramref name="sources"/> instance.
	/// </returns>
	/// <remarks>
	/// Null, empty and whitespace-only strings are treated as absent. This method returns either
	/// the shared `TimestampSources.Empty` singleton or the same reference passed in; it does not
	/// create a new copy of a non-empty instance.
	/// </remarks>
	public static TimestampSources Normalize(TimestampSources? sources)
	{
		if(sources is null) return Empty;
		sources = sources with
		{
			Date = NormalizeField(sources.Date),
			Time = NormalizeField(sources.Time),
			SubSeconds = NormalizeField(sources.SubSeconds),
			Offset = NormalizeField(sources.Offset),
			DateTime = NormalizeField(sources.DateTime),
			DateTimeOffset = NormalizeField(sources.DateTimeOffset),
			Timestamp = NormalizeField(sources.Timestamp)
		};

		return sources.IsEmpty ? Empty : sources;
	}
	private static string? NormalizeField(string? value)
	{
		if(value is null) return null;
		string trimmed = value.Trim();
		return trimmed.Length == 0 ? null : trimmed;
	}
}