using BMTP3.Core.BackupSource;
using System.Diagnostics;

namespace BMTP3.Core.Configs {
	[DebuggerDisplay("{Title}, {Name}")]
	public class DeviceSourceConfig : BaseSourceConfig {
		public override SourceType SourceType => SourceType.Device;
	}
}
