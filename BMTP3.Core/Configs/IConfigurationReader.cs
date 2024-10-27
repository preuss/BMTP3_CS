using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Configs {
	public interface IConfigurationReader {
		IBackupSettings ReadConfiguration(string filePath);
		IBackupSettings GetDefaultConfiguration();
		Dictionary<string, IBackupSettings> GetDeviceConfigurations();
		string GetPropertyNameImpl(MemberInfo member);
	}
}
