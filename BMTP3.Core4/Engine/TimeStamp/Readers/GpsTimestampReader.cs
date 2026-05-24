using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Definitions;
using MetadataExtractor.Formats.Exif;

namespace BMTP3.Core4.Engine.TimeStamp.Readers;

internal sealed class GpsTimestampReader : BaseDirectoryTimestampReader<GpsDirectory>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.Gps;

	protected override TimestampSourceType SourceType => TimestampSourceType.Exif;
}