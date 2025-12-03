using BMTP3.Core2.BackupNew2.Models.Configuration;
using BMTP3.Core2.BackupNew2.Models.Configuration.Enums;
using System.Diagnostics;

namespace BMTP3.Core2.BackupNew2.Configs
{
	[DebuggerDisplay("{Title}, {Name}")]
	public class DeviceSourceConfig : BaseSourceConfig
	{
		public override SourceType SourceType => SourceType.MediaDevice;
	}
}
