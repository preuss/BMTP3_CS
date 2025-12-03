namespace BMTP3.Core2.Configs
{
	internal class DeviceConfig
	{
		private string deviceName;
		public DeviceConfig(string deviceName)
		{
			this.deviceName = deviceName;
		}
		public string DeviceName
		{
			get { return deviceName; }
		}
	}
}
