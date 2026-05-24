using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Definitions;
using MetadataExtractor.Formats.Exif;

namespace BMTP3.Core4.Engine.TimeStamp.Readers;

internal sealed class ExifTimestampReader : BaseDirectoryTimestampReader<ExifDirectoryBase>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.Exif;

	protected override TimestampSourceType SourceType => TimestampSourceType.Exif;
}