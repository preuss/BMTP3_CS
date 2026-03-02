using BMTP3.Core2.BackupNew.candidates;
using BMTP3.Core2.BackupNew.exifreader.definitions;
using MetadataExtractor.Formats.QuickTime;

namespace BMTP3.Core2.BackupNew.exifreader.readers;

public class QuickTimeMovieHeaderTimestampReader : BaseDirectoryTimestampReader<QuickTimeMovieHeaderDirectory>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.QuickTimeMovieHeader;

	protected override TimestampSourceType SourceType => TimestampSourceType.QuickTime;
}
