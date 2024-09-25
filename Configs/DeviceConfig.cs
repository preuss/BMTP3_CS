using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Configs {
	internal class DeviceConfig {
		private string deviceName;
		public DeviceConfig(string deviceName) {
			this.deviceName = deviceName;
		}
		public string DeviceName {
			get { return this.deviceName; }
		}
	}
}
