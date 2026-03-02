using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.candidates;

/// <summary>
/// Defines what happened at a given point in time (a time-related event).
/// Can represent times from file system metadata (Windows/Linux) or from media metadata
/// (EXIF, XMP, IPTC, QuickTime, profiles).
///
/// Use this enum to categorize all timestamps consistently across different sources.
/// </summary>
public enum TimestampRole
{
	/// <summary>
	/// Created timestamp.
	///
	/// Sources:
	/// - Windows FileInfo: <c>CreationTime</c>
	/// - Linux/Unix: <c>btime</c> (birth time, if available)
	/// - EXIF: <c>DateTimeOriginal</c>
	/// - IPTC: <c>DateCreated</c> / <c>TimeCreated</c>
	/// - XMP: <c>xmp:CreateDate</c>, <c>photoshop:DateCreated</c>, <c>exif:DateTimeOriginal</c>
	/// - QuickTime: movie header created time, metadata header creation date
	/// - GPS: <c>DateStamp</c> + <c>TimeStamp</c> (represents capture/creation-like time)
	///
	/// Description:
	/// The time when the object/media was created or came into existence (best available "creation-like" timestamp).
	/// </summary>
	Created,

	/// <summary>
	/// Modified timestamp.
	///
	/// Sources:
	/// - Windows FileInfo: <c>LastWriteTime</c>
	/// - Linux/Unix: <c>mtime</c> (modification time)
	/// - EXIF: <c>DateTime</c> (often "ModifyDate")
	/// - XMP: <c>xmp:ModifyDate</c>
	/// - QuickTime: movie header modified time
	///
	/// Description:
	/// The time when the object/content was last modified.
	/// </summary>
	Modified,

	/// <summary>
	/// Accessed timestamp.
	///
	/// Sources:
	/// - Windows FileInfo: <c>LastAccessTime</c>
	/// - Linux/Unix: <c>atime</c> (last access time)
	///
	/// Description:
	/// The time when the object was last accessed/read. Does not imply modification.
	/// </summary>
	Accessed,

	/// <summary>
	/// Metadata modified timestamp.
	///
	/// Sources:
	/// - XMP: <c>xmp:MetadataDate</c>
	/// - Linux/Unix: <c>ctime</c> (inode metadata change time; permissions/owner/metadata changes)
	///
	/// Description:
	/// Tracks changes to metadata/attributes without necessarily implying a content modification.
	/// </summary>
	MetadataModified,

	/// <summary>
	/// Digitized timestamp.
	///
	/// Sources:
	/// - EXIF: <c>DateTimeDigitized</c>
	/// - IPTC: <c>DigitalDateCreated</c> / <c>DigitalTimeCreated</c>
	/// - XMP: <c>exif:DateTimeDigitized</c>
	///
	/// Description:
	/// The time when analog media was digitized or scanned into digital form.
	/// </summary>
	Digitized,

	/// <summary>
	/// Profile created timestamp.
	///
	/// Sources:
	/// - EXIF/XMP/IPTC/containers: processing history entries or embedded profile creation timestamps (if present)
	///
	/// Description:
	/// The time when a processing/profile entry was created. Availability depends on file format and software.
	/// </summary>
	ProfileCreated,

	/// <summary>
	/// Unknown or unmapped timestamp.
	///
	/// Sources:
	/// - Any timestamp that cannot be mapped to a specific <see cref="TimestampRole"/>
	///
	/// Description:
	/// Use when the timestamp exists but its meaning is unclear.
	/// </summary>
	Unknown
}
