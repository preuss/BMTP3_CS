using BMTP3.Consoles.candidates;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Icc;
using MetadataExtractor.Formats.Png;
using MetadataExtractor.Formats.QuickTime;
using MetadataExtractor.Formats.Xmp;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BMTP3.Consoles.exifreader;

public static class DateTimeFormatConstants
{
	public static readonly string[] DateFormats =
	{
		"yyyy-MM-dd",
		"yyyy/MM/dd",
		"yyyy.MM.dd",
		"dd-MM-yyyy",
		"dd/MM/yyyy",
		"MM-dd-yyyy",
		"MM/dd/yyyy"
	};

	public static readonly string[] TimeFormats =
	{
		"HH:mm:ss",
		"HH:mm",
		"HH.mm.ss",
		"HH.mm",
		"mm:ss",
		"mm:ss.fff"
	};

	public static readonly string[] DateTimeFormats =
	{
		"yyyy:MM:dd HH:mm:ss.fffzzz",
		"yyyy:MM:dd HH:mm:sszzz",
		"yyyy:MM:dd HH:mm:ss.fff",
		"yyyy:MM:dd HH:mm:ss",
		"yyyy-MM-ddTHH:mm:ss.fffzzz",
		"yyyy-MM-ddTHH:mm:sszzz",
		"yyyy-MM-ddTHH:mm:ss.fff",
		"yyyy-MM-ddTHH:mm:ss",
		"yyyy:MM:dd HH:mm:ss zzz",
		"yyyy:MM:dd HH:mm:ss.fff zzz"
	};
}

public class ExifReader
{
	private const string PhotoshopNs = "http://ns.adobe.com/photoshop/1.0/";

	public ExifReader() { }

	public void ReadExifData(string filePath, bool showAllTags = false)
	{
		Console.WriteLine($"Reading EXIF data from: {filePath}");

		FileInfo fileInfo = new FileInfo(filePath);
		SortedDictionary<string, TimestampCandidate> fileDates = GetFileSystemDates(fileInfo);
		PrintKeyValueSection("---- System (FileSystem) ----", fileDates);

		using FileStream stream = File.OpenRead(filePath);
		IReadOnlyList<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(stream, filePath);

		SortedDictionary<string, string> dateValues = ExtractAllDates(directories);
		PrintKeyValueSection("---- Dates (MetadataExtractor) ----", dateValues);

		IReadOnlyList<DateTimeOffset> timestamps = ExtractTimestamps(directories, fileInfo);
		Console.WriteLine("---- Parsed Timestamp Candidates ----");
		if(timestamps.Count == 0)
		{
			Console.WriteLine("(none)");
		} else
		{
			foreach(DateTimeOffset timestamp in timestamps)
			{
				Console.WriteLine($"  {timestamp:O}");
			}
			Console.WriteLine($"Oldest: {timestamps.Min():O}");
		}

		if(!showAllTags) return;

		Console.WriteLine();
		Console.WriteLine("---- All tags ----");
		foreach(MetadataExtractor.Directory directory in directories)
		{
			Console.WriteLine($"Directory: {directory.Name}");
			if(directory.Errors.Count > 0)
			{
				Console.WriteLine("  Errors:");
				foreach(string error in directory.Errors)
				{
					Console.WriteLine($"    {error}");
				}
			}

			if(directory is XmpDirectory xmpDir)
			{
				Console.WriteLine("  (XMP data present)");
				IDictionary<string, string> props = xmpDir.GetXmpProperties() ?? new Dictionary<string, string>();
				Console.WriteLine($"  XMP Properties: {props.Count}");
				foreach(KeyValuePair<string, string> prop in props)
				{
					Console.WriteLine($"    {prop.Key} = {prop.Value}");
				}
			}

			foreach(Tag tag in directory.Tags)
			{
				Console.WriteLine($"  Type: {tag.Type}, Name: {tag.Name} = {tag.Description}");
			}
		}
	}

	/// <summary>
	/// Extract all timestamps from metadata directories and optional file info.
	/// Returns a sorted distinct list of UTC <see cref="DateTimeOffset"/>.
	/// </summary>
	public IReadOnlyList<DateTimeOffset> ExtractTimestamps(IReadOnlyList<MetadataExtractor.Directory> directories, FileInfo? fileInfo = null)
	{
		ArgumentNullException.ThrowIfNull(directories);
		List<DateTimeOffset> list = new();
		CollectFileSystemTimestamps(fileInfo, list);
		CollectKnownMetadataTimestamps(directories, list);
		CollectPngTimestamps(directories, list);
		CollectGenericTagTimestamps(directories, list);
		return list.Distinct().OrderBy(d => d).ToList();
	}

	/// <summary>
	/// Get the oldest timestamp discovered in the file metadata or filesystem.
	/// </summary>
	public DateTimeOffset? GetOldestTimestampFromFile(string filePath)
	{
		if(string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath must be provided", nameof(filePath));
		FileInfo fileInfo = new(filePath);
		using FileStream stream = File.OpenRead(filePath);
		IReadOnlyList<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(stream, filePath);
		return ExtractTimestamps(directories, fileInfo).FirstOrDefault();
	}

	// collection helpers
	private static void CollectFileSystemTimestamps(FileInfo? fileInfo, List<DateTimeOffset> list)
	{
		if(fileInfo is null || !fileInfo.Exists) return;
		list.Add(ToUtcDateTimeOffset(fileInfo.CreationTimeUtc));
		list.Add(ToUtcDateTimeOffset(fileInfo.LastWriteTimeUtc));
		list.Add(ToUtcDateTimeOffset(fileInfo.LastAccessTimeUtc));
	}

	private static void CollectKnownMetadataTimestamps(IReadOnlyList<MetadataExtractor.Directory> directories, List<DateTimeOffset> list)
	{
		XmpDirectory? xmp = directories.OfType<XmpDirectory>().FirstOrDefault();
		if(xmp?.XmpMeta is not null)
		{
			TryAddXmpProp(xmp, PhotoshopNs, "DateCreated", list);
			TryAddXmpProp(xmp, Schema.XmpProperties, "CreateDate", list);
			TryAddXmpProp(xmp, Schema.XmpProperties, "ModifyDate", list);
		}

		IccDirectory? icc = directories.OfType<IccDirectory>().FirstOrDefault();
		if(icc is not null && icc.TryGetDateTime(IccDirectory.TagProfileDateTime, out DateTime iccDt))
		{
			list.Add(new DateTimeOffset(DateTime.SpecifyKind(iccDt, DateTimeKind.Unspecified)));
		}

		QuickTimeMovieHeaderDirectory? quickTime = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
		if(quickTime is not null)
		{
			if(quickTime.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out DateTime qtCreated)) list.Add(new DateTimeOffset(DateTime.SpecifyKind(qtCreated, DateTimeKind.Unspecified)));
			if(quickTime.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagModified, out DateTime qtModified)) list.Add(new DateTimeOffset(DateTime.SpecifyKind(qtModified, DateTimeKind.Unspecified)));
		}

		foreach(ExifSubIfdDirectory subIfd in directories.OfType<ExifSubIfdDirectory>())
		{
			TryAddExifDateTag(subIfd, ExifDirectoryBase.TagDateTimeOriginal, list);
			TryAddExifDateTag(subIfd, ExifDirectoryBase.TagDateTimeDigitized, list);
		}

		foreach(ExifIfd0Directory ifd0 in directories.OfType<ExifIfd0Directory>())
		{
			TryAddExifDateTag(ifd0, ExifDirectoryBase.TagDateTime, list);
		}
	}

	private static void CollectPngTimestamps(IReadOnlyList<MetadataExtractor.Directory> directories, List<DateTimeOffset> list)
	{
		foreach(PngDirectory png in directories.OfType<PngDirectory>())
		{
			foreach(Tag? tag in png.Tags.Where(t => t.Type == PngDirectory.TagLastModificationTime)) TryAddParsedDateString(tag.Description, list);
			foreach(Tag? tag in png.Tags.Where(t => t.Type == PngDirectory.TagTextualData))
			{
				if(string.IsNullOrWhiteSpace(tag.Description)) continue;
				foreach(string value in EnumeratePossibleDateStringsFromPngText(tag.Description)) TryAddParsedDateString(value, list);
			}
		}
	}

	private static void CollectGenericTagTimestamps(IReadOnlyList<MetadataExtractor.Directory> directories, List<DateTimeOffset> list)
	{
		foreach(MetadataExtractor.Directory directory in directories)
		{
			foreach(Tag? tag in directory.Tags)
			{
				if(string.IsNullOrWhiteSpace(tag.Description)) continue;
				if(directory is PngDirectory && tag.Type == PngDirectory.TagTextualData) continue;
				TryAddParsedDateString(tag.Description, list);
			}
		}
	}

	private static IEnumerable<string> EnumeratePossibleDateStringsFromPngText(string text)
	{
		string[] lines = text.Split(new[] { '\r', '\n', '\0' }, StringSplitOptions.RemoveEmptyEntries);
		foreach(string rawLine in lines)
		{
			string line = rawLine.Trim();
			if(string.IsNullOrEmpty(line)) continue;
			if(line.StartsWith("date:create", StringComparison.OrdinalIgnoreCase) || line.StartsWith("date:modify", StringComparison.OrdinalIgnoreCase))
			{
				int firstColon = line.IndexOf(':');
				if(firstColon < 0) continue;
				int secondColon = line.IndexOf(':', firstColon + 1);
				yield return secondColon >= 0 ? line.Substring(secondColon + 1).Trim() : line.Substring(firstColon + 1).Trim();
				continue;
			}
			yield return line;
		}
	}

	private static DateTimeOffset ToUtcDateTimeOffset(DateTime utc)
	{
		if(utc.Kind == DateTimeKind.Unspecified) return new DateTimeOffset(utc);
		return utc.ToUniversalTime();
	}

	private static void TryAddParsedDateString(string? raw, List<DateTimeOffset> list)
	{
		if(string.IsNullOrWhiteSpace(raw)) return;
		if(DateTimeParser.TryParseAny(raw, out DateTimeOffset dto)) list.Add(dto);
	}

	private static void TryAddXmpProp(XmpDirectory xmp, string ns, string propName, List<DateTimeOffset> list)
	{
		try { string raw = xmp.XmpMeta.GetPropertyString(ns, propName); TryAddParsedDateString(raw, list); } catch { /* ignore */ }
	}

	private static void TryAddExifDateTag(ExifDirectoryBase directory, int tagType, List<DateTimeOffset> list)
	{
		if(directory is null) return;
		if(directory.SafeTryGetDateTime(tagType, out DateTime dt))
		{
			list.Add(new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified)));
		}
	}

	// map helpers
	private static void AddIfHasValue(SortedDictionary<string, string> map, string label, string? value)
	{
		if(string.IsNullOrWhiteSpace(value)) return;
		map[label] = value;
	}

	private static void AddDateTime(SortedDictionary<string, string> map, string label, ExifDirectoryBase? directory, int tagType)
	{
		if(directory is null) return;
		if(directory.SafeTryGetDateTime(tagType, out DateTime dt))
		{
			AddIfHasValue(map, label, dt.ToString("O", CultureInfo.InvariantCulture));
		}
	}

	private static void AddDateOnly(SortedDictionary<string, string> map, string label, ExifDirectoryBase? directory, int tagType)
	{
		if(directory is null) return;
		if(directory.SafeTryGetDateTime(tagType, out DateTime dt))
		{
			AddIfHasValue(map, label, DateOnly.FromDateTime(dt).ToString("O", CultureInfo.InvariantCulture));
		}
	}

	private static void AddTimeOnly(SortedDictionary<string, string> map, string label, ExifDirectoryBase? directory, int tagType)
	{
		if(directory is null) return;
		if(directory.SafeTryGetDateTime(tagType, out DateTime dt))
		{
			AddIfHasValue(map, label, TimeOnly.FromDateTime(dt).ToString("O", CultureInfo.InvariantCulture));
		}
	}

	private static void AddSubSecDateTime(SortedDictionary<string, string> map, string label, ExifSubIfdDirectory? directory, int dateTimeTag, int subSecTag)
	{
		if(!directory.SafeTryGetDateTime(dateTimeTag, out DateTime baseDateTime)) return;
		string? subSec = directory.SafeGetString(subSecTag);
		if(string.IsNullOrWhiteSpace(subSec)) return;
		if(!int.TryParse(subSec.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int ms)) return;
		ms = Math.Clamp(ms, 0, 999);
		DateTime dt = new DateTime(baseDateTime.Year, baseDateTime.Month, baseDateTime.Day, baseDateTime.Hour, baseDateTime.Minute, baseDateTime.Second, baseDateTime.Kind).AddMilliseconds(ms);
		AddIfHasValue(map, label, dt.ToString("O", CultureInfo.InvariantCulture));
	}

	private static void AddGpsDateTime(SortedDictionary<string, string> map, string label, GpsDirectory? gps)
	{
		if(gps is null) return;
		try { if(!gps.TryGetDateTime(GpsDirectory.TagDateStamp, out DateTime dvalue)) return; if(!gps.TryGetDateTime(GpsDirectory.TagTimeStamp, out DateTime tvalue)) return; AddIfHasValue(map, label, $"{DateOnly.FromDateTime(dvalue).ToString("O", CultureInfo.InvariantCulture)} {TimeOnly.FromDateTime(tvalue).ToString("O", CultureInfo.InvariantCulture)}"); } catch { }
	}

	private static void AddXmpDateIfPresent(SortedDictionary<string, string> map, string label, XmpDirectory xmp, string schemaNamespace, string propName)
	{
		try
		{
			string value = xmp.XmpMeta.GetPropertyString(schemaNamespace, propName);
			if(string.IsNullOrWhiteSpace(value)) return;
			if(DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset dto)) { AddIfHasValue(map, label, dto.ToString("O", CultureInfo.InvariantCulture)); return; }
			AddIfHasValue(map, label, value);
		} catch { }
	}

	private static void AddExifToolLikeDateTimeOffset(SortedDictionary<string, string> map, string label, ExifDirectoryBase? exif, int dateTimeTag, int? subSecTag, TimeSpan? offset)
	{
		if(exif is null || offset is null) return;
		if(!exif.SafeTryGetDateTime(dateTimeTag, out DateTime baseDateTime)) return;
		int milliseconds = 0;
		if(subSecTag is not null)
		{
			string? subSec = exif.SafeGetString(subSecTag.Value);
			if(!string.IsNullOrWhiteSpace(subSec) && int.TryParse(subSec.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int ms)) milliseconds = Math.Clamp(ms, 0, 999);
		}
		DateTime dt = new DateTime(baseDateTime.Year, baseDateTime.Month, baseDateTime.Day, baseDateTime.Hour, baseDateTime.Minute, baseDateTime.Second, DateTimeKind.Unspecified).AddMilliseconds(milliseconds);
		DateTimeOffset dto = new DateTimeOffset(dt, offset.Value);
		AddIfHasValue(map, label, dto.ToString("O", CultureInfo.InvariantCulture));
	}

	private static void ExtractPartsFromRaw(string raw, out string? dateRaw, out string? timeRaw, out string? offsetRaw, out string? subSecondsRaw)
	{
		dateRaw = null; timeRaw = null; offsetRaw = null; subSecondsRaw = null;
		if(string.IsNullOrWhiteSpace(raw)) return;
		raw = raw.Trim();
		string[] tokens = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		int idx = 0; while(idx < tokens.Length && !IsDateToken(tokens[idx])) idx++;
		if(idx < tokens.Length) dateRaw = string.Join(' ', tokens, 0, idx);
		while(idx < tokens.Length) { if(IsTimeToken(tokens[idx])) { timeRaw = tokens[idx]; idx++; break; } idx++; }
		if(!string.IsNullOrWhiteSpace(timeRaw)) { while(idx < tokens.Length) { if(IsZoneOffsetToken(tokens[idx])) { offsetRaw = tokens[idx]; idx++; } else if(tokens[idx].StartsWith('.')) { subSecondsRaw = tokens[idx].Substring(1); idx++; } else break; } }
		if(string.IsNullOrWhiteSpace(dateRaw) && !string.IsNullOrWhiteSpace(timeRaw) && (subSecondsRaw is not null || offsetRaw is not null)) { dateRaw = timeRaw; timeRaw = null; }
	}

	private static DateOnly? ParseDateOnlyFromRaw(string? dateRaw)
	{
		if(string.IsNullOrWhiteSpace(dateRaw)) return null;
		dateRaw = dateRaw.Trim();
		foreach(string format in DateTimeFormatConstants.DateFormats) if(DateOnly.TryParseExact(dateRaw, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly result)) return result;
		if(DateTime.TryParse(dateRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt)) return DateOnly.FromDateTime(dt);
		return null;
	}

	private static TimeOnly? ParseTimeOnlyFromRaw(string? timeRaw)
	{
		if(string.IsNullOrWhiteSpace(timeRaw)) return null;
		timeRaw = timeRaw.Trim();
		foreach(string format in DateTimeFormatConstants.TimeFormats) if(TimeOnly.TryParseExact(timeRaw, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly result)) return result;
		if(DateTime.TryParse(timeRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt)) return TimeOnly.FromDateTime(dt);
		return null;
	}

	private static int? ParseSubSecondsToMilliseconds(string? subSecondsRaw)
	{
		if(string.IsNullOrWhiteSpace(subSecondsRaw)) return null;
		subSecondsRaw = subSecondsRaw.Trim();
		if(double.TryParse(subSecondsRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out double floatMs)) return (int)Math.Round(Math.Clamp(floatMs, 0, 999), 0, MidpointRounding.AwayFromZero);
		return null;
	}

	private static TimeSpan? ParseOffsetFromRaw(string? offsetRaw)
	{
		if(string.IsNullOrWhiteSpace(offsetRaw)) return null;
		offsetRaw = offsetRaw.Trim();
		if(TimeSpan.TryParseExact(offsetRaw, @"hh\:mm", CultureInfo.InvariantCulture, TimeSpanStyles.None, out TimeSpan offset)) return offset;
		return null;
	}

	private static bool IsDateToken(string token) => Regex.IsMatch(token, @"^\d{4}[-/]\d{1,2}[-/]\d{1,2}$");
	private static bool IsTimeToken(string token) => Regex.IsMatch(token, @"^\d{1,2}:\d{2}:\d{2}(\.\d+)?$");
	private static bool IsZoneOffsetToken(string token) => Regex.IsMatch(token, @"^[+\-]\d{1,2}:\d{2}$");

	private static SortedDictionary<string, TimestampCandidate> GetFileSystemDates(FileInfo fileInfo)
	{
		ArgumentNullException.ThrowIfNull(fileInfo);
		SortedDictionary<string, TimestampCandidate> map = new SortedDictionary<string, TimestampCandidate>(StringComparer.OrdinalIgnoreCase);
		if(!fileInfo.Exists) return map;
		
		DateTime creationDateTimeUtc = fileInfo.CreationTimeUtc;
		map["File Creation Time"] = TimestampCandidateFactory.FromDateTime(creationDateTimeUtc, TimestampSourceType.FileSystem, TimestampRole.Creation);

		DateTime lastWriteDateTimeUtc = fileInfo.LastWriteTimeUtc;
		map["File Last Write Time"] = TimestampCandidateFactory.FromDateTime(lastWriteDateTimeUtc, TimestampSourceType.FileSystem, TimestampRole.Modification);

		DateTime lastAccessDateTimeUtc = fileInfo.LastAccessTimeUtc;
		map["File Last Access Time"] = TimestampCandidateFactory.FromDateTime(lastAccessDateTimeUtc, TimestampSourceType.FileSystem, TimestampRole.Unknown);

		return map;
	}

	private static void PrintKeyValueSection(string title, SortedDictionary<string, TimestampCandidate> map)
	{
		if(map is null || map.Count == 0)
		{
			Console.WriteLine(title);
			Console.WriteLine("(none)");
			return;
		}

		Console.WriteLine(title);
		foreach(KeyValuePair<string, TimestampCandidate> item in map)
		{
			Console.WriteLine($"{item.Key,-32}: {FormatTimestampCandidate(item.Value)}");
		}
	}

	private static void PrintKeyValueSection(string title, SortedDictionary<string, string> map)
	{
		if(map is null || map.Count == 0)
		{
			Console.WriteLine(title);
			Console.WriteLine("(none)");
			return;
		}

		Console.WriteLine(title);
		foreach(KeyValuePair<string, string> item in map)
		{
			Console.WriteLine($"{item.Key,-32}: {item.Value}");
		}
	}

	private static string FormatExifOffset(TimeSpan offset)
	{
		string sign = offset < TimeSpan.Zero ? "-" : "+";
		offset = offset.Duration();

		string result = $"{sign}{offset.Hours:D2}:{offset.Minutes:D2}";

		long fractionTicks = offset.Ticks % TimeSpan.TicksPerSecond;
		bool needsSeconds = offset.Seconds != 0 || fractionTicks != 0;

		if(needsSeconds)
		{
			result += $":{offset.Seconds:D2}";

			if(fractionTicks != 0)
			{
				string fraction = ((int)fractionTicks).ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0');
				result += $".{fraction}";
			}
		}

		return result;
	}

	private static bool TryParseExifOffset(string value, out TimeSpan offset)
	{
		offset = default;
		if(string.IsNullOrWhiteSpace(value)) return false;
		value = value.Trim();
		// Expect leading '+' or '-'
		int sign = 1;
		if(value[0] == '+') sign = 1;
		else if(value[0] == '-') sign = -1;
		else return false;
		string remainder = value.Substring(1);
		// Accept formats: "hh:mm", "hh:mm:ss", "hh:mm:ss.FFF..." (up to 7 fractional digits)
		string[] formats = new[] { @"hh\:mm\:ss\.FFFFFFF", @"hh\:mm\:ss\.fff", @"hh\:mm\:ss", @"hh\:mm" };
		if(TimeSpan.TryParseExact(remainder, formats, CultureInfo.InvariantCulture, out TimeSpan ts))
		{
			offset = sign == 1 ? ts : -ts;
			return true;
		}
		return false;
	}

	private static TimeSpan? ParseOffsetFromRawFlexible(string? offsetRaw)
	{
		if(string.IsNullOrWhiteSpace(offsetRaw)) return null;
		offsetRaw = offsetRaw.Trim();
		string[] formats = new[] { @"hh\:mm\:ss\.FFFFFFF", @"hh\:mm\:ss\.fff", @"hh\:mm\:ss", @"hh\:mm" };
		if(TimeSpan.TryParseExact(offsetRaw, formats, CultureInfo.InvariantCulture, out TimeSpan ts)) return ts;
		return null;
	}

	private static SortedDictionary<string, string> ExtractAllDates(IReadOnlyList<MetadataExtractor.Directory> directories)
	{
		ArgumentNullException.ThrowIfNull(directories);
		SortedDictionary<string, string> map = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		XmpDirectory? xmp = directories.OfType<XmpDirectory>().FirstOrDefault();
		ExifIfd0Directory? ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
		AddDateTime(map, "EXIF Modify Date", ifd0, ExifDirectoryBase.TagDateTime);
		ExifSubIfdDirectory? subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
		AddDateTime(map, "EXIF Date/Time Original", subIfd, ExifDirectoryBase.TagDateTimeOriginal);
		AddSubSecDateTime(map, "EXIF Date/Time Original (SubSec)", subIfd, ExifDirectoryBase.TagDateTimeOriginal, ExifDirectoryBase.TagSubsecondTimeOriginal);
		AddDateTime(map, "EXIF Create Date", subIfd, ExifDirectoryBase.TagDateTimeDigitized);
		AddSubSecDateTime(map, "EXIF Create Date (SubSec)", subIfd, ExifDirectoryBase.TagDateTimeDigitized, ExifDirectoryBase.TagSubsecondTimeDigitized);
		TimeSpan? offsetModify = TryGetExifOffset(subIfd, ExifDirectoryBase.TagTimeZone);
		TimeSpan? offsetOriginal = TryGetExifOffset(subIfd, ExifDirectoryBase.TagTimeZoneOriginal) ?? offsetModify;
		TimeSpan? offsetDigitized = TryGetExifOffset(subIfd, ExifDirectoryBase.TagTimeZoneDigitized) ?? offsetModify;
		AddExifToolLikeDateTimeOffset(map, "EXIF Modify Date (Offset)", ifd0, ExifDirectoryBase.TagDateTime, null, offsetModify);
		AddExifToolLikeDateTimeOffset(map, "EXIF Date/Time Original (Offset)", subIfd, ExifDirectoryBase.TagDateTimeOriginal, ExifDirectoryBase.TagSubsecondTimeOriginal, offsetOriginal);
		AddExifToolLikeDateTimeOffset(map, "EXIF Create Date (Offset)", subIfd, ExifDirectoryBase.TagDateTimeDigitized, ExifDirectoryBase.TagSubsecondTimeDigitized, offsetDigitized);
		GpsDirectory? gps = directories.OfType<GpsDirectory>().FirstOrDefault();
		AddDateOnly(map, "GPS Date Stamp", gps, GpsDirectory.TagDateStamp);
		AddTimeOnly(map, "GPS Time Stamp", gps, GpsDirectory.TagTimeStamp);
		AddGpsDateTime(map, "GPS Date/Time", gps);
		IccDirectory? icc = directories.OfType<IccDirectory>().FirstOrDefault();
		if(icc is not null)
		{
			AddIfHasValue(map, "ICC Profile Date Time", icc.GetDescription(IccDirectory.TagProfileDateTime));
			if(icc.TryGetDateTime(IccDirectory.TagProfileDateTime, out DateTime iccDt)) AddIfHasValue(map, "ICC Profile Date Time (Parsed)", iccDt.ToString("O", CultureInfo.InvariantCulture));
		}
		if(xmp?.XmpMeta is not null)
		{
			AddXmpDateIfPresent(map, "XMP Create Date", xmp, Schema.XmpProperties, "CreateDate");
			AddXmpDateIfPresent(map, "XMP Modify Date", xmp, Schema.XmpProperties, "ModifyDate");
			AddXmpDateIfPresent(map, "XMP Date Created", xmp, PhotoshopNs, "DateCreated");
		}
		foreach(PngDirectory? png in directories.OfType<PngDirectory>())
		{
			foreach(Tag? tag in png.Tags.Where(t => t.Type == PngDirectory.TagLastModificationTime))
			{
				if(DateTimeParser.TryParseAny(tag.Description ?? string.Empty, out TimeSpan parsedTime)) AddIfHasValue(map, "PNG-tIME Last Modification Time", parsedTime.ToString("O", CultureInfo.InvariantCulture));
			}
			foreach(Tag? tag in png.Tags.Where(t => t.Type == PngDirectory.TagTextualData))
			{
				if(string.IsNullOrWhiteSpace(tag.Description)) continue;
				foreach(string value in EnumeratePossibleDateStringsFromPngText(tag.Description)) if(DateTimeParser.TryParseAny(value, out DateTimeOffset parsed)) AddIfHasValue(map, "PNG Textual Date", parsed.ToString("O", CultureInfo.InvariantCulture));
			}
		}
		foreach(MetadataExtractor.Directory directory in directories)
		{
			foreach(Tag? tag in directory.Tags)
			{
				if(string.IsNullOrWhiteSpace(tag.Description) || string.IsNullOrWhiteSpace(tag.Name)) continue;
				if(directory is PngDirectory && tag.Type == PngDirectory.TagTextualData) continue;
				if(DateTimeParser.TryParseAny(tag.Description, out DateTimeOffset parsed)) AddIfHasValue(map, $"{directory.Name} {tag.Name}", parsed.ToString("O", CultureInfo.InvariantCulture));
			}
		}
		List<TimestampCandidate> candidates = CollectTimestampCandidates(directories);
		Console.WriteLine("---- Timestamp Candidates ----");
		foreach(TimestampCandidate c in candidates)
		{
			Console.WriteLine($"  {c.MetaSource} / {c.TimeKind}: {FormatTimestampCandidate(c)}");
		}

		// Merge candidate-derived entries into the map
		SortedDictionary<string, string> candidateMap = BuildMapFromCandidates(candidates);
		foreach(KeyValuePair<string, string> kv in candidateMap)
		{
			if(!map.ContainsKey(kv.Key))
			{
				map[kv.Key] = kv.Value;
			}
		}
		return map;
	}

	private static TimeSpan? TryGetExifOffset(MetadataExtractor.Directory? directory, int tagType)
	{
		if(directory is null) return null;
		string? raw = null;
		try
		{
			raw = directory.SafeGetString(tagType);
		} catch
		{
			return null;
		}
		if(string.IsNullOrWhiteSpace(raw)) return null;
		return TryParseExifOffset(raw, out TimeSpan offset) ? offset : null;
	}

	private static List<TimestampCandidate> CollectTimestampCandidates(IReadOnlyList<MetadataExtractor.Directory> directories)
	{
		List<TimestampCandidate> candidates = new List<TimestampCandidate>();
		ExifSubIfdDirectory? subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
		ExifIfd0Directory? ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
		IccDirectory? icc = directories.OfType<IccDirectory>().FirstOrDefault();
		XmpDirectory? xmp = directories.OfType<XmpDirectory>().FirstOrDefault();
		QuickTimeMovieHeaderDirectory? quickTime = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
		AddExifCandidate(candidates, "Exif", "DateTimeOriginal", subIfd, ExifDirectoryBase.TagDateTimeOriginal, ExifDirectoryBase.TagSubsecondTimeOriginal, ExifDirectoryBase.TagTimeZoneOriginal);
		AddExifCandidate(candidates, "Exif", "DateTimeDigitized", subIfd, ExifDirectoryBase.TagDateTimeDigitized, ExifDirectoryBase.TagSubsecondTimeDigitized, ExifDirectoryBase.TagTimeZoneDigitized);
		AddExifCandidate(candidates, "Exif", "DateTime", ifd0, ExifDirectoryBase.TagDateTime, subSecondTagType: null, timeZoneTagType: ExifDirectoryBase.TagTimeZone);
		AddXmpCandidate(candidates, xmp, "Xmp", "CreateDate", Schema.XmpProperties, "CreateDate");
		AddXmpCandidate(candidates, xmp, "Xmp", "ModifyDate", Schema.XmpProperties, "ModifyDate");
		AddXmpCandidate(candidates, xmp, "Xmp", "DateCreated", PhotoshopNs, "DateCreated");
		if(icc is not null && icc.TryGetDateTime(IccDirectory.TagProfileDateTime, out DateTime iccDt)) candidates.Add(new TimestampCandidate("Icc", "ProfileDateTime", DateOnly.FromDateTime(iccDt), iccDt.ToString("O", CultureInfo.InvariantCulture), TimeOnly.FromDateTime(iccDt), iccDt.ToString("O", CultureInfo.InvariantCulture), Offset: null, OffsetRaw: null, SubSeconds: null, SubSecondsRaw: null, Raw: iccDt.ToString("O", CultureInfo.InvariantCulture)));
		if(quickTime is not null)
		{
			if(quickTime.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out DateTime qtCreated)) candidates.Add(new TimestampCandidate("QuickTime", "Created", DateOnly.FromDateTime(qtCreated), qtCreated.ToString(), TimeOnly.FromDateTime(qtCreated), qtCreated.ToString(), null, null, null, null, qtCreated.ToString()));
			if(quickTime.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagModified, out DateTime qtModified)) candidates.Add(new TimestampCandidate("QuickTime", "Modified", DateOnly.FromDateTime(qtModified), qtModified.ToString(), TimeOnly.FromDateTime(qtModified), qtModified.ToString(), null, null, null, null, qtModified.ToString()));
		}
		return candidates;
	}

	private static string FormatTimestampCandidate(TimestampCandidate c)
	{
		if(!string.IsNullOrWhiteSpace(c.Raw))
			return c.Raw!;

		if(c.Date.HasValue && c.Time.HasValue)
		{
			string time = c.Time.Value.ToString("HH:mm:ss");
			if(c.SubSeconds.HasValue)
			{
				time += "." + c.SubSeconds.Value.ToString("D3");
			}

			string offset = string.Empty;
			if(!string.IsNullOrWhiteSpace(c.OffsetRaw))
			{
				// preserve original precision (may include seconds/milliseconds)
				offset = c.OffsetRaw!;
			} else if(c.Offset.HasValue)
			{
				offset = FormatExifOffset(c.Offset.Value);
			}

			return c.Date.Value.ToString("yyyy-MM-dd") + "T" + time + offset;
		}

		if(c.Date.HasValue) return c.Date.Value.ToString("yyyy-MM-dd");
		if(c.Time.HasValue)
		{
			string time = c.Time.Value.ToString("HH:mm:ss");
			if(c.SubSeconds.HasValue) time += "." + c.SubSeconds.Value.ToString("D3");
			return time;
		}

		return string.Empty;
	}

	// Convert a list of TimestampCandidate into a simple key->string map
	private static SortedDictionary<string, string> BuildMapFromCandidates(IEnumerable<TimestampCandidate> candidates)
	{
		SortedDictionary<string, string> map = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach(TimestampCandidate c in candidates)
		{
			if(string.IsNullOrWhiteSpace(c.MetaSource) || string.IsNullOrWhiteSpace(c.TimeKind))
			{
				continue;
			}
			string key = $"{c.MetaSource} {c.TimeKind}";
			string value = FormatTimestampCandidate(c);
			if(string.IsNullOrWhiteSpace(value)) continue;
			if(!map.ContainsKey(key))
			{
				map[key] = value;
			}
		}
		return map;
	}

	private static void AddExifCandidate(List<TimestampCandidate> candidates, string source, string kind, ExifDirectoryBase? directory, int dateTimeTagType, int? subSecondTagType, int? timeZoneTagType)
	{
		if(directory is null) return;
		if(!directory.TryGetDateTime(dateTimeTagType, out DateTime dt)) return;

		int subMs = 0;
		bool hasSub = false;
		if(subSecondTagType is not null)
		{
			try
			{
				string? sub = directory.SafeGetString(subSecondTagType.Value);
				if(!string.IsNullOrWhiteSpace(sub) && int.TryParse(sub.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int ms))
				{
					subMs = Math.Clamp(ms, 0, 999);
					hasSub = true;
				}
			} catch { }
		}

		DateTime baseDt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, DateTimeKind.Unspecified).AddMilliseconds(subMs);
		TimeSpan offset = TimeSpan.Zero;
		bool hasOffset = false;
		if(timeZoneTagType is not null)
		{
			try
			{
				string? tz = directory.SafeGetString(timeZoneTagType.Value);
				if(!string.IsNullOrWhiteSpace(tz) && TryParseExifOffset(tz, out TimeSpan parsedOffset))
				{
					offset = parsedOffset;
					hasOffset = true;
				}
			} catch { }
		}

		DateOnly date = DateOnly.FromDateTime(baseDt);
		TimeOnly time = TimeOnly.FromDateTime(baseDt);
		int? subSeconds = hasSub ? subMs : null;
		TimeSpan? offsetNullable = hasOffset ? offset : null;
		string? rawDateTime = null;
		try { rawDateTime = directory.SafeGetString(dateTimeTagType); } catch { }
		string? subRaw = null;
		if(subSecondTagType is not null) { try { subRaw = directory.SafeGetString(subSecondTagType.Value); } catch { } }
		string? tzRaw = null;
		if(timeZoneTagType is not null) { try { tzRaw = directory.SafeGetString(timeZoneTagType.Value); } catch { } }
		string? offsetRawToUse = tzRaw ?? (offsetNullable.HasValue ? FormatExifOffset(offsetNullable.Value) : null);
		candidates.Add(new TimestampCandidate(source, kind, date, rawDateTime, time, rawDateTime, offsetNullable, offsetRawToUse, subSeconds, subRaw, rawDateTime));
	}

	private static void AddXmpCandidate(List<TimestampCandidate> candidates, XmpDirectory? xmp, string source, string kind, string schemaNamespace, string propName)
	{
		if(xmp?.XmpMeta is null) return;
		try
		{
			string? raw = xmp.XmpMeta.GetPropertyString(schemaNamespace, propName);
			if(string.IsNullOrWhiteSpace(raw)) return;

			if(!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _)) return;

			ExtractPartsFromRaw(raw, out string? dateRaw, out string? timeRaw, out string? offsetRaw, out string? subRaw);

			// Parse components.
			DateOnly? date = ParseDateOnlyFromRaw(dateRaw);
			TimeOnly? time = ParseTimeOnlyFromRaw(timeRaw);
			int? subSeconds = ParseSubSecondsToMilliseconds(subRaw);
			TimeSpan? off = ParseOffsetFromRaw(offsetRaw);

			TimestampSources sources = new()
			{
				Date = dateRaw,
				Time = timeRaw,
				SubSeconds = subRaw,
				Offset = offsetRaw,
				DateTime = null,
				DateTimeOffset = null
			};

			string? finalRaw = !string.IsNullOrWhiteSpace(dateRaw) && !string.IsNullOrWhiteSpace(timeRaw) ? raw.Trim() : null;
			candidates.Add(new TimestampCandidate(
				sourceType: source,
				role: kind,
				sources: sources,
				date: date,
				time: time,
				subSeconds: subSeconds,
				offset: off
			));
		} catch { }
	}
}
