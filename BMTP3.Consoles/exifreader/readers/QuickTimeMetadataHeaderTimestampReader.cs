using BMTP3.Consoles.candidates;
using BMTP3.Consoles.exifreader.definitions;
using MetadataExtractor.Formats.QuickTime;
using System.Collections.Generic;

namespace BMTP3.Consoles.exifreader.readers;

public class QuickTimeMetadataHeaderTimestampReader : BaseDirectoryTimestampReader<QuickTimeMetadataHeaderDirectory>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.QuickTimeMetadataHeader;

	protected override TimestampSourceType SourceType => TimestampSourceType.QuickTime;
}
