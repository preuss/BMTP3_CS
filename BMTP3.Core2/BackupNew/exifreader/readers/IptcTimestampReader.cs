using BMTP3.Core2.BackupNew.candidates;
using BMTP3.Core2.BackupNew.exifreader.definitions;
using MetadataExtractor.Formats.Iptc;

namespace BMTP3.Core2.BackupNew.exifreader.readers;

public class IptcTimestampReader : BaseDirectoryTimestampReader<IptcDirectory>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.IptcTagDefinitions;

	protected override TimestampSourceType SourceType => TimestampSourceType.Iptc;
}