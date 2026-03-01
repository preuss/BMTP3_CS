using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.candidates;
public enum TimestampSourceType
{
	FileSystem,
	Exif,
	Xmp,
	Icc,
	QuickTime,
	Iptc
}