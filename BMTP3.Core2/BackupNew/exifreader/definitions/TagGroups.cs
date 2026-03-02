using BMTP3.Core2.BackupNew.candidates;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.QuickTime;

namespace BMTP3.Core2.BackupNew.exifreader.definitions;
/// <summary>
/// Provides factual mappings of metadata tags grouped by their respective directories.
/// </summary>
public static class TagGroups
{
	// ----------------------------------------------------------------
	// SECTION A: Integer-based Directories
	// ----------------------------------------------------------------

	public static readonly List<TimestampTagGroup> Exif = new()
	{
		// Created
		new(TimestampRole.Created,
			ExifDirectoryBase.TagDateTimeOriginal,
			null,
			ExifDirectoryBase.TagSubsecondTimeOriginal,
			ExifDirectoryBase.TagTimeZoneOriginal),

        // Digitized (Scanner/Sensor processing)
        new(TimestampRole.Digitized,
			ExifDirectoryBase.TagDateTimeDigitized,
			null,
			ExifDirectoryBase.TagSubsecondTimeDigitized,
			ExifDirectoryBase.TagTimeZoneDigitized),

        // Modification (File modified by software)
        new(TimestampRole.Modified,
			ExifDirectoryBase.TagDateTime,
			null,
			ExifDirectoryBase.TagSubsecondTime,
			ExifDirectoryBase.TagTimeZone)
	};

	public static readonly List<TimestampTagGroup> IptcTagDefinitions = new()
	{
		new(TimestampRole.Created,
			DateTag: IptcDirectory.TagDateCreated,
			TimeTag: IptcDirectory.TagTimeCreated
		),
		new(
			TimestampRole.Digitized,
			DateTag: IptcDirectory.TagDigitalDateCreated,
			TimeTag: IptcDirectory.TagDigitalTimeCreated
		)
	};

	public static readonly List<TimestampTagGroup> Gps = new()
	{
		new(TimestampRole.Created,
			DateTag: GpsDirectory.TagDateStamp,
			TimeTag: GpsDirectory.TagTimeStamp
		),
	};

	public static readonly List<TimestampTagGroup> QuickTimeMovieHeader = new()
	{
		// Standard QuickTime Atom (UTC)
		new(TimestampRole.Created,
			QuickTimeMovieHeaderDirectory.TagCreated),

		new(TimestampRole.Modified,
			QuickTimeMovieHeaderDirectory.TagModified)
	};

	public static readonly List<TimestampTagGroup> QuickTimeMetadataHeader = new()
	{
		// Modern Apple/iPhone metadata
		new(TimestampRole.Created,
			QuickTimeMetadataHeaderDirectory.TagCreationDate)
	};

	// ----------------------------------------------------------------
	// SECTION B: String-based XMP
	// ----------------------------------------------------------------

	public const string NsXmp = "http://ns.adobe.com/xap/1.0/";
	public const string NsExif = "http://ns.adobe.com/exif/1.0/";
	public const string NsPhotoshop = "http://ns.adobe.com/photoshop/1.0/";
	public const string NsTiff = "http://ns.adobe.com/tiff/1.0/";

	public static readonly List<XmpTagDefinition> Xmp = new()
	{
        // 1. EXIF Namespace (Highest priority for capture)
        new(TimestampRole.Created, NsExif, "DateTimeOriginal"),
		new(TimestampRole.Digitized, NsExif, "DateTimeDigitized"),

        // 2. Photoshop Namespace
        new(TimestampRole.Created, NsPhotoshop, "DateCreated"),
        // 3. XMP Basic Namespace
        new(TimestampRole.Created, NsXmp, "CreateDate"), // In XMP, CreateDate is often digitization
        new(TimestampRole.Modified, NsXmp, "ModifyDate"),
		new(TimestampRole.MetadataModified, NsXmp, "MetadataDate"),
        
        // 4. TIFF Namespace (Sometimes used for fallback)
        new(TimestampRole.Modified, NsTiff, "DateTime")
	};
}