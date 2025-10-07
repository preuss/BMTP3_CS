using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.Services;
internal class SystemClock : IClock {
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
