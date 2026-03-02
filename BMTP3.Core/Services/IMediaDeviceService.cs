using MediaDevices;

namespace BMTP3.Core.Services {
	public interface IMediaDeviceService {
		/// <summary>
		/// Returns an enumerable collection of currently available portable devices.
		/// </summary>
		/// <returns>>An enumerable collection of portable devices currently available.</returns>
		public IEnumerable<MediaDevice> GetDevices();
		/// <summary>
		/// Returns an enumerable collection of currently available private portable devices.
		/// </summary>
		/// <returns>>An enumerable collection of private portable devices currently available.</returns>
		public IEnumerable<MediaDevice> GetPrivateDevices();
	}
}
