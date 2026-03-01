using BMTP3.Consoles.candidates;
using BMTP3.Consoles.exifreader.definitions;
using MetadataExtractor.Formats.Exif;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.exifreader.readers;

public class GpsTimestampReader : BaseDirectoryTimestampReader<GpsDirectory>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.Gps;

	protected override TimestampSourceType SourceType => TimestampSourceType.Exif;
}
