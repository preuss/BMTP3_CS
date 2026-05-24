using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Definitions;
using MetadataExtractor.Formats.Iptc;

namespace BMTP3.Core4.Engine.TimeStamp.Readers;

internal sealed class IptcTimestampReader : BaseDirectoryTimestampReader<IptcDirectory>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.IptcTagDefinitions;

	protected override TimestampSourceType SourceType => TimestampSourceType.Iptc;
}