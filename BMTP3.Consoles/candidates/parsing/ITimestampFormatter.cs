using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.candidates.parsing;

public interface ITimestampFormatter
{
	string Format(TimestampCandidate candidate, TimestampFormatStyle formatStyle);
	string Format(TimestampCandidate candidate, TimestampFormatDescriptor descriptor);
}
