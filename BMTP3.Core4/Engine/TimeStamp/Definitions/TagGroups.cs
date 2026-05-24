using BMTP3.Core4.Engine.TimeStamp.Candidates;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.QuickTime;

namespace BMTP3.Core4.Engine.TimeStamp.Definitions;

/// <summary>
///     Provides factual mappings of metadata tags grouped by their respective directories.
/// </summary>
public static class TagGroups
{
	// ----------------------------------------------------------------
	// SECTION B: String-based XMP
	// ----------------------------------------------------------------

	public const string NsXmp = "http://ns.adobe.com/xap/1.0/";
	public const string NsExif = "http://ns.adobe.com/exif/1.0/";
	public const string NsPhotoshop = "http://ns.adobe.com/photoshop/1.0/";

	public const string NsTiff = "http://ns.adobe.com/tiff/1.0/";
	// ----------------------------------------------------------------
	// SECTION A: Integer-based Directories
	// ----------------------------------------------------------------

	public static readonly List<TimestampTagGroup> Exif = new()
	{
		// Created
		new TimestampTagGroup(TimestampRole.Created,
			ExifDirectoryBase.TagDateTimeOriginal,
			null,
			ExifDirectoryBase.TagSubsecondTimeOriginal,
			ExifDirectoryBase.TagTimeZoneOriginal),

		// Digitized (Scanner/Sensor processing)
		new TimestampTagGroup(TimestampRole.Digitized,
			ExifDirectoryBase.TagDateTimeDigitized,
			null,
			ExifDirectoryBase.TagSubsecondTimeDigitized,
			ExifDirectoryBase.TagTimeZoneDigitized),

		// Modification (File modified by software)
		new TimestampTagGroup(TimestampRole.Modified,
			ExifDirectoryBase.TagDateTime,
			null,
			ExifDirectoryBase.TagSubsecondTime,
			ExifDirectoryBase.TagTimeZone)
	};

	public static readonly List<TimestampTagGroup> IptcTagDefinitions = new()
	{
		new TimestampTagGroup(TimestampRole.Created,
			IptcDirectory.TagDateCreated,
			IptcDirectory.TagTimeCreated
		),
		new TimestampTagGroup(
			TimestampRole.Digitized,
			IptcDirectory.TagDigitalDateCreated,
			IptcDirectory.TagDigitalTimeCreated
		)
	};

	public static readonly List<TimestampTagGroup> Gps = new()
	{
		new TimestampTagGroup(TimestampRole.Created,
			GpsDirectory.TagDateStamp,
			GpsDirectory.TagTimeStamp
		)
	};

	public static readonly List<TimestampTagGroup> QuickTimeMovieHeader = new()
	{
		// Standard QuickTime Atom (UTC)
		new TimestampTagGroup(TimestampRole.Created,
			QuickTimeMovieHeaderDirectory.TagCreated),

		new TimestampTagGroup(TimestampRole.Modified,
			QuickTimeMovieHeaderDirectory.TagModified)
	};

	public static readonly List<TimestampTagGroup> QuickTimeMetadataHeader = new()
	{
		// Modern Apple/iPhone metadata
		new TimestampTagGroup(TimestampRole.Created,
			QuickTimeMetadataHeaderDirectory.TagCreationDate)
	};

	public static readonly List<XmpTagDefinition> Xmp = new()
	{
		// 1. EXIF Namespace (Highest priority for capture)
		new XmpTagDefinition(TimestampRole.Created, NsExif, "DateTimeOriginal"),
		new XmpTagDefinition(TimestampRole.Digitized, NsExif, "DateTimeDigitized"),

		// 2. Photoshop Namespace
		new XmpTagDefinition(TimestampRole.Created, NsPhotoshop, "DateCreated"),
		// 3. XMP Basic Namespace
		new XmpTagDefinition(TimestampRole.Created, NsXmp, "CreateDate"), // In XMP, CreateDate is often digitization
		new XmpTagDefinition(TimestampRole.Modified, NsXmp, "ModifyDate"),
		new XmpTagDefinition(TimestampRole.MetadataModified, NsXmp, "MetadataDate"),

		// 4. TIFF Namespace (Sometimes used for fallback)
		new XmpTagDefinition(TimestampRole.Modified, NsTiff, "DateTime")
	};
}