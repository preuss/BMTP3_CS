using BMTP3.Core.BackupSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Configs {
	public class DriveSourceConfig : BaseSourceConfig {
		public override SourceType SourceType { get; } = SourceType.Drive;
	}
}
