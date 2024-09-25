using MediaDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Services {
	public class MediaDeviceServiceTest : IMediaDeviceService {
		public IEnumerable<MediaDevice> GetDevices() {
			throw new NotImplementedException();
		}
		public IEnumerable<MediaDevice> GetPrivateDevices() {
			throw new NotImplementedException();
		}
	}
}
