using BMTP3.Consoles.candidates;
using BMTP3.Consoles.exifreader.definitions;
using MetadataExtractor.Formats.QuickTime;
using System.Collections.Generic;

namespace BMTP3.Consoles.exifreader.readers;

public class QuickTimeMovieHeaderTimestampReader : BaseDirectoryTimestampReader<QuickTimeMovieHeaderDirectory>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.QuickTimeMovieHeader;

	protected override TimestampSourceType SourceType => TimestampSourceType.QuickTime;
}
