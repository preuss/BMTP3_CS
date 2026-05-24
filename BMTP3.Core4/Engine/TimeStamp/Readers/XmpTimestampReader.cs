using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Definitions;
using BMTP3.Core4.Engine.TimeStamp.Parsers;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using Directory = MetadataExtractor.Directory;

namespace BMTP3.Core4.Engine.TimeStamp.Readers;

internal sealed class XmpTimestampReader : ITimestampReader
{
	private static readonly DateTimeOffsetParser _dateTimeOffsetParser = new();
	private static readonly DateTimeParser _dateTimeParser = new();
	private static readonly DateWithFullParser _dateParser = new();
	private static readonly DateWithYearMonthParser _dateWithYearMonthParser = new();
	private static readonly DateWithYearParser _dateWithYearParser = new();

	public IReadOnlyList<TimestampCandidate> Read(FileInfo fileInfo, CancellationToken cancellationToken)
	{
		List<TimestampCandidate> candidates = new();
		IEnumerable<Directory> directories;

		try
		{
			using Stream stream = fileInfo.OpenRead();
			directories = ImageMetadataReader.ReadMetadata(stream);
		}
		catch
		{
			return Array.Empty<TimestampCandidate>();
		}

		foreach (XmpDirectory xmpDir in directories.OfType<XmpDirectory>())
		{
			if (xmpDir.XmpMeta == null)
			{
				continue;
			}

			foreach (XmpTagDefinition tagDef in TagGroups.Xmp)
			{
				string? rawValue = GetXmpPropertyStringOrNull(xmpDir, tagDef.Namespace, tagDef.PropertyName);
				if (string.IsNullOrWhiteSpace(rawValue))
				{
					continue;
				}

				TimestampCandidate? candidate = ParseXmpValueOrNull(tagDef.Role, rawValue);
				if (candidate != null)
				{
					candidates.Add(candidate);
				}
			}
		}

		return candidates;
	}

	private static string? GetXmpPropertyStringOrNull(XmpDirectory xmpDir, string ns, string propertyName)
	{
		if (xmpDir.XmpMeta is null)
		{
			return null;
		}

		try
		{
			return xmpDir.XmpMeta.GetPropertyString(ns, propertyName);
		}
		catch
		{
			return null;
		}
	}

	private TimestampCandidate? ParseXmpValueOrNull(TimestampRole role, string rawValue)
	{
		if (_dateTimeOffsetParser.TryParse(rawValue, out DateTimeOffset dto))
		{
			return TimestampCandidateFactory.FromRawDateTimeOffset(
				TimestampSourceType.Xmp,
				role,
				rawValue,
				dto
			);
		}

		if (_dateTimeParser.TryParse(rawValue, out DateTime dt))
		{
			return TimestampCandidateFactory.FromRawDateTime(
				TimestampSourceType.Xmp,
				role,
				rawValue,
				dt
			);
		}

		if (TryParseDateWithResolution(rawValue, out DateOnly date, out ChronoDateResolution resolution))
		{
			return TimestampCandidateFactory.FromRawDate(
				TimestampSourceType.Xmp,
				role,
				rawValue,
				date,
				resolution
			);
		}

		return null;
	}

	private bool TryParseDateWithResolution(string? rawDate, out DateOnly date, out ChronoDateResolution resolution)
	{
		if (_dateParser.TryParse(rawDate, out DateOnly fullDate))
		{
			date = fullDate;
			resolution = ChronoDateResolution.FullDate;
			return true;
		}

		if (_dateWithYearMonthParser.TryParse(rawDate, out DateOnly yearMonth))
		{
			date = yearMonth;
			resolution = ChronoDateResolution.YearAndMonth;
			return true;
		}

		if (_dateWithYearParser.TryParse(rawDate, out DateOnly yearOnly))
		{
			date = yearOnly;
			resolution = ChronoDateResolution.YearOnly;
			return true;
		}

		date = default;
		resolution = default;
		return false;
	}
}