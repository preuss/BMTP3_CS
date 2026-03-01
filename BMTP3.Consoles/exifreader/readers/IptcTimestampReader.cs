using BMTP3.Consoles.candidates;
using BMTP3.Consoles.exifreader.definitions;
using MetadataExtractor.Formats.Iptc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.exifreader.readers;

public class IptcTimestampReader : BaseDirectoryTimestampReader<IptcDirectory>
{
	protected override IEnumerable<TimestampTagGroup> TagDefinitions => TagGroups.IptcTagDefinitions;

	protected override TimestampSourceType SourceType => TimestampSourceType.Iptc;
}