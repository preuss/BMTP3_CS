using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Definitions;
using MetadataExtractor.Formats.QuickTime;

namespace BMTP3.Core4.Engine.TimeStamp.Readers;

internal sealed class QuickTimeMovieHeaderTimestampReader : BaseDirectoryTimestampReader<QuickTimeMovieHeaderDirectory>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.QuickTimeMovieHeader;

	protected override TimestampSourceType SourceType => TimestampSourceType.QuickTime;
}