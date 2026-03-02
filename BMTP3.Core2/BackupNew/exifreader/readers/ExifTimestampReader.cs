using BMTP3.Core2.BackupNew.candidates;
using BMTP3.Core2.BackupNew.exifreader.definitions;
using MetadataExtractor.Formats.Exif;

namespace BMTP3.Core2.BackupNew.exifreader.readers;

public class ExifTimestampReader : BaseDirectoryTimestampReader<ExifDirectoryBase>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.Exif;

	protected override TimestampSourceType SourceType => TimestampSourceType.Exif;
}
