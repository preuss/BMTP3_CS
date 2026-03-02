using BMTP3.Core2.BackupNew.candidates;
using BMTP3.Core2.BackupNew.exifreader.definitions;
using MetadataExtractor.Formats.Exif;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.exifreader.readers;

public class ExifTimestampReader : BaseDirectoryTimestampReader<ExifDirectoryBase>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.Exif;

	protected override TimestampSourceType SourceType => TimestampSourceType.Exif;
}
