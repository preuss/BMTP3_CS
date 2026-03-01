using BMTP3.Consoles.candidates;
using BMTP3.Consoles.exifreader.parsers;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.QuickTime;
using MetadataExtractor.Formats.Xmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.exifreader.definitions;
/// <summary>
/// Definition for directory types that use Integer IDs (EXIF, IPTC, GPS, QuickTime).
/// These metadata tags represents together a tiemstamp through separate string fields 
/// (Date, Time, SubSec, Offset) or a unified numeric timestamp.
/// DateTag can contain date only, date+time, or date+time+offset, supporting incremental parsing.
/// </summary>
public record TimestampTagGroup(
	TimestampRole Role,

	// Composite timestamp components
	int? DateTag = null,      // Used for: "2024:05:20" or "2024:05:20 14:30:05" or "2024:05:20 14:30:05+02:00"
	int? TimeTag = null,      // Used for: "14:30:05"
	int? SubSecTag = null,    // Used for: "921" (0x9291 etc.)
	int? OffsetTag = null,    // Used for: "+02:00"

	// Unified numeric timestamp
	int? TimestampTag = null, // Used for: 1716213005 (Unix/Mac/Ticks)
	EpochType TimestampEpoch = EpochType.Unix,
	TimestampResolution TimestampResolution = TimestampResolution.Seconds
);
