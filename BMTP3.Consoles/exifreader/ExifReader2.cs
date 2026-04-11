using BMTP3.Core2.BackupNew.candidates;
using MetadataExtractor.Formats.Exif;

namespace BMTP3.Consoles.exifreader;

public class ExifReader2
{
	public IReadOnlyList<TimestampCandidate> Read(ExifDirectoryBase? exif)
	{
		if (exif is null)
		{
			return Array.Empty<TimestampCandidate>();
		}

		List<TimestampCandidate> result = new();

		// ── Original capture time
		AddExifDateTime(
			result,
			exif,
			ExifDirectoryBase.TagDateTimeOriginal,
			TimestampRole.Created,
			"Exif:DateTimeOriginal"
		);

		// ── Digitized time
		AddExifDateTime(
			result,
			exif,
			ExifDirectoryBase.TagDateTimeDigitized,
			TimestampRole.Digitized,
			"Exif:DateTimeDigitized"
		);

		// ── General DateTime (often last modified by camera)
		AddExifDateTime(
			result,
			exif,
			ExifDirectoryBase.TagDateTime,
			TimestampRole.Modified,
			"Exif:DateTime"
		);

		return result;
	}

	private static void AddExifDateTime(
		List<TimestampCandidate> list,
		ExifDirectoryBase exif,
		int tag,
		TimestampRole role,
		string sourceLabel
	)
	{
		string? raw = exif.SafeGetString(tag);

		if (string.IsNullOrWhiteSpace(raw))
		{
			return;
		}

		// 1) Preserve raw exactly as delivered
		TimestampSources sources = new()
		{
			DateTime = raw.Trim()
		};

		// 2) Parse according to Exif rules
		if (!ExifDateTimeParser.TryParse(
			    raw,
			    out DateOnly? date,
			    out TimeOnly? time))
		{
			// raw is kept, but parsing failed → still a candidate
			list.Add(new TimestampCandidate(
				TimestampSourceType.Exif,
				role,
				sources,
				null,
				null,
				null,
				null,
				null
			));
			return;
		}

		// 3) Successful parse
		list.Add(new TimestampCandidate(
			TimestampSourceType.Exif,
			role,
			sources,
			date,
			ChronoDateResolution.FullDate,
			time,
			null,
			null
		));
	}
}