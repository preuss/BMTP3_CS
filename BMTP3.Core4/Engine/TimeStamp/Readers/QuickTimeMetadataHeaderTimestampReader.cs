using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Definitions;
using MetadataExtractor.Formats.QuickTime;

namespace BMTP3.Core4.Engine.TimeStamp.Readers;

internal sealed class QuickTimeMetadataHeaderTimestampReader : BaseDirectoryTimestampReader<QuickTimeMetadataHeaderDirectory>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.QuickTimeMetadataHeader;

	protected override TimestampSourceType SourceType => TimestampSourceType.QuickTime;
}