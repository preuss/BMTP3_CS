using BMTP3.Core2.BackupNew2.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.Configs {
	[DebuggerDisplay("{Title}, {Name}")]
	public class DeviceSourceConfig : BaseSourceConfig {
		public override SourceType SourceType => SourceType.MediaDevice;
	}
}
