using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;

namespace BMTP3_CS.Configs {
	public class ConfigModel : ITomlMetadataProvider {
		private readonly IDictionary<string, object> _metadata = new Dictionary<string, object>();

		public bool HasMetadata => _metadata.Count > 0;

		public IDictionary<string, object> Metadata => _metadata;

		public DefaultSourceConfig Default { get; set; } = new DefaultSourceConfig();

		public IList<DeviceSourceConfig> DeviceSources { get; set; } = new List<DeviceSourceConfig>();
		public IList<DriveSourceConfig> DriveSources { get; set; } = new List<DriveSourceConfig>();

		TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
	}
}
