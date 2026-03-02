using System.Reflection;

namespace BMTP3.Core.Configs {
	public interface IConfigurationReader {
		IBackupSettings ReadConfiguration(string filePath);
		IBackupSettings GetDefaultConfiguration();
		Dictionary<string, IBackupSettings> GetDeviceConfigurations();
		string GetPropertyNameImpl(MemberInfo member);
	}
}
