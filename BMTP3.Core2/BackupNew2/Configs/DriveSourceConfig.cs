using BMTP3.Core2.BackupNew2.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.Configs {
	public class DriveSourceConfig : BaseSourceConfig {
		public override SourceType SourceType { get; } = SourceType.FileSystem;
	}
}
