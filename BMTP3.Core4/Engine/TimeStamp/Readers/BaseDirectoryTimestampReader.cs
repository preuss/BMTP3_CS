using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Definitions;
using BMTP3.Core4.Engine.TimeStamp.Extensions;
using BMTP3.Core4.Engine.TimeStamp.Parsers;
using MetadataExtractor;
using Directory = MetadataExtractor.Directory;

namespace BMTP3.Core4.Engine.TimeStamp.Readers;

internal abstract class BaseDirectoryTimestampReader<TDirectory> : ITimestampReader where TDirectory : Directory
{
	private static readonly DateTimeOffsetParser _dateTimeOffsetParser = new();
	private static readonly DateTimeParser _dateTimeParser = new();
	private static readonly DateWithFullParser _dateParser = new();
	private static readonly DateWithYearMonthParser _dateWithYearMonthParser = new();
	private static readonly DateWithYearParser _dateWithYearParser = new();
	private static readonly TimeParser _timeParser = new();
	private static readonly OffsetParser _offsetParser = new();
	private static readonly SubSecondParser _subSecondParser = new();
	private static readonly TimestampParser _timestampParser = new();

	protected abstract IEnumerable<TimestampTagGroup> TagDefinitions { get; }
	protected abstract TimestampSourceType SourceType { get; }

	public IReadOnlyList<TimestampCandidate> Read(FileInfo fileInfo, CancellationToken cancellationToken)
	{
		List<TimestampCandidate> candidates = new();
		IEnumerable<Directory> directories;

		try
		{
			using Stream stream = fileInfo.OpenRead();
			directories = ImageMetadataReader.ReadMetadata(stream);
		} catch
		{
			return Array.Empty<TimestampCandidate>();
		}

		foreach(TDirectory dir in directories.OfType<TDirectory>())
		{
			foreach(TimestampTagGroup group in TagDefinitions)
			{
				string? rawDate = GetStringForTag(dir, group.DateTag);
				string? rawTime = GetStringForTag(dir, group.TimeTag);
				string? rawOffset = GetStringForTag(dir, group.OffsetTag);
				string? rawSubSec = GetStringForTag(dir, group.SubSecTag);

				string? rawTimestamp = GetStringForTag(dir, group.TimestampTag);
				long? timestampValue = GetLongForTag(dir, group.TimestampTag);

				TimestampCandidate? timestampCandidate = CreateCandidateFromTimestamp(group, rawTimestamp, timestampValue);

				TimestampCandidate? dateCandidate = CreateCandidateFromDates(group, rawDate, rawTime, rawOffset, rawSubSec);

				TimestampCandidate? candidate = dateCandidate ?? timestampCandidate;
				if(candidate != null)
				{
					candidates.Add(candidate);
				}
			}
		}

		return candidates;
	}

	private TimestampCandidate? CreateCandidateFromTimestamp(
		TimestampTagGroup group,
		string? rawTimestamp,
		long? timestampValue
	)
	{
		if(!timestampValue.HasValue && _timestampParser.TryParse(rawTimestamp, out long parsedTimestamp))
		{
			timestampValue = parsedTimestamp;
		}

		if(!timestampValue.HasValue)
		{
			return null;
		}

		TimestampResolution timestampResolution = TimestampResolutionEvaluator.DetermineEffectiveResolution(
			timestampValue.Value,
			group.TimestampResolution
		);

		return TimestampCandidateFactory.FromRawTimestamp(
			SourceType,
			group.Role,
			rawTimestamp,
			timestampValue.Value,
			group.TimestampEpoch,
			timestampResolution
		);
	}

	private TimestampCandidate? CreateCandidateFromDates(
		TimestampTagGroup group,
		string? rawDate,
		string? rawTime,
		string? rawOffset,
		string? rawSubSec
	)
	{
		if(string.IsNullOrWhiteSpace(rawDate))
		{
			return null;
		}

		TimeOnly? time = _timeParser.ParseOrNull(rawTime);
		TimeSpan? offset = _offsetParser.ParseOrNull(rawOffset);
		long? subSec = _subSecondParser.ParseOrNull(rawSubSec);

		// Try DateTimeOffset
		if(_dateTimeOffsetParser.TryParse(rawDate, out DateTimeOffset dto))
		{
			return TimestampCandidateFactory.FromDateTimeOffsetWithSubSec(
				SourceType, group.Role, rawDate, rawSubSec, dto, subSec
			);
		}

		// Try DateTime
		if(_dateTimeParser.TryParse(rawDate, out DateTime dt))
		{
			return TimestampCandidateFactory.FromRawDateTimeWithOffsetAndSubSec(
				SourceType, group.Role, rawDate, rawOffset, rawSubSec, dt, offset, subSec
			);
		}

		// Try DateOnly
		if(TryParseDateWithResolution(rawDate, out DateOnly date, out ChronoDateResolution dateRes))
		{
			return TimestampCandidateFactory.FromRawDateWithTimeAndOffSetAndSubSec(
				SourceType, group.Role, rawDate, rawTime, rawOffset, rawSubSec, date, dateRes, time, offset, subSec
			);
		}

		return null;
	}

	private bool TryParseDateWithResolution(string? rawDate, out DateOnly date, out ChronoDateResolution resolution)
	{
		if(_dateParser.TryParse(rawDate, out DateOnly fullDate))
		{
			date = fullDate;
			resolution = ChronoDateResolution.FullDate;
			return true;
		}

		if(_dateWithYearMonthParser.TryParse(rawDate, out DateOnly yearMonth))
		{
			date = yearMonth;
			resolution = ChronoDateResolution.YearAndMonth;
			return true;
		}

		if(_dateWithYearParser.TryParse(rawDate, out DateOnly yearOnly))
		{
			date = yearOnly;
			resolution = ChronoDateResolution.YearOnly;
			return true;
		}

		date = default;
		resolution = default;
		return false;
	}

	private string? GetStringForTag(TDirectory dir, int? tag)
	{
		if(!tag.HasValue)
		{
			return null;
		}

		return dir.SafeGetString(tag.Value);
	}

	private long? GetLongForTag(TDirectory dir, int? tag)
	{
		if(tag.HasValue && dir.TryGetInt64(tag.Value, out long val))
		{
			return val;
		}

		return null;
	}
}