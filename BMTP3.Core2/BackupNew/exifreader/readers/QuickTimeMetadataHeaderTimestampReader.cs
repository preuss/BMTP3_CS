using BMTP3.Core2.BackupNew.candidates;
using BMTP3.Core2.BackupNew.exifreader.definitions;
using MetadataExtractor.Formats.QuickTime;
using System.Collections.Generic;

namespace BMTP3.Core2.BackupNew.exifreader.readers;

public class QuickTimeMetadataHeaderTimestampReader : BaseDirectoryTimestampReader<QuickTimeMetadataHeaderDirectory>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.QuickTimeMetadataHeader;

	protected override TimestampSourceType SourceType => TimestampSourceType.QuickTime;
}
