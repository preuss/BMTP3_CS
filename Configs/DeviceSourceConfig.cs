using BMTP3_CS.BackupSource;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Configs {
	[DebuggerDisplay("{Title}, {Name}")]
	public class DeviceSourceConfig : BaseSourceConfig {
		public override SourceType SourceType => SourceType.Device;
	}
}
