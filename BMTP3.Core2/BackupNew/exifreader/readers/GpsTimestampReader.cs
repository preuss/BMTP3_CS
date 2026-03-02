using BMTP3.Core2.BackupNew.candidates;
using BMTP3.Core2.BackupNew.exifreader.definitions;
using MetadataExtractor.Formats.Exif;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.exifreader.readers;

public class GpsTimestampReader : BaseDirectoryTimestampReader<GpsDirectory>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.Gps;

	protected override TimestampSourceType SourceType => TimestampSourceType.Exif;
}
