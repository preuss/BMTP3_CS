using System.Collections.Immutable;
using Tomlyn.Model;
using Tomlyn;
using Tomlyn.Syntax;
using Tomlyn.Helpers;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using BMTP3_CS.Options;
using BMTP3_CS.Exceptions;

namespace BMTP3_CS.Configs {
	internal class BackupSettingsReader {
		//private readonly string configFilePath;
		private IBackupSettings? backupSettings;
		private ConfigModel? deviceConfigModel;

		public BackupSettingsReader() {
			//this.configFilePath = configFilePath;
			//backupSettings = LoadConfig3();
		}

		//public string ConfigFilePath => configFilePath;

		public ConfigModel LoadDeviceConfigModel(FileInfo configFileInfo) {
			TomlModelOptions modelOptions = CreateModelOptions();

			ConfigModel? deviceConfigModel;
			DiagnosticsBag? diagnostics;
			try {
				//DeviceConfigModel deviceConfigModel = Toml.ToModel<DeviceConfigModel>(File.ReadAllText(configFilePath), null, modelOptions);
				if(false == Toml.TryToModel<ConfigModel>(File.ReadAllText(configFileInfo.FullName), out deviceConfigModel, out diagnostics, null, modelOptions)) {
					if(diagnostics == null) {
						throw new Exception("Error reading Config File: " + configFileInfo.FullName);
					} else if(diagnostics.HasErrors) {
						Console.WriteLine("Configfile has errors : " + configFileInfo.FullName);
						Console.WriteLine("Config Reader has errors Count: " + diagnostics.Count);

						foreach(var diagnostic in diagnostics) {
							Console.WriteLine(diagnostic.Message);
						}
						throw new Exception("Configfile has errors : " + configFileInfo.FullName);
					}
				}
			} catch(FileNotFoundException ex) {
				throw new BackupConfigFileNotFoundException($"Kunne ikke finde konfigurationsfilen '{configFileInfo.FullName}'");
			}
			if(deviceConfigModel == null) {
				throw new Exception("Error reading Config File: " + configFileInfo.FullName);
			}
			return deviceConfigModel;
		}

		private void UpdateWithDefaultValuesFrom(ConfigModel? deviceConfigModel) {
			if(deviceConfigModel == null) {
				return;
			}
			if(deviceConfigModel.Default == null) {
				return;
			}
			foreach(var deviceSource in deviceConfigModel.DeviceSources) {
				if(!deviceConfigModel.Default.Disabled) { deviceSource.Enabled = false; }
				deviceSource.CompareByBinary ??= deviceConfigModel.Default.CompareByBinary;
				if(!deviceConfigModel.Default.Recursive) { deviceSource.Recursive = false; }
				deviceSource.UseFilePattern ??= deviceConfigModel.Default.UseFilePattern;
				if(string.IsNullOrWhiteSpace(deviceSource.FilePattern)) { deviceSource.FilePattern = deviceConfigModel.Default.FilePattern; }
				if(string.IsNullOrWhiteSpace(deviceSource.FilePatternIfExist)) { deviceSource.FilePatternIfExist = deviceConfigModel.Default.FilePatternIfExist; }
			}
		}

		public DefaultSourceConfig GetDefaultConfig() {
			if(this.deviceConfigModel == null) {
				return new DefaultSourceConfig();
			} else {
				return this.deviceConfigModel.Default;
			}
		}

		public IList<DeviceSourceConfig> GetDeviceConfigs() {
			if(this.deviceConfigModel != null) {
				return deviceConfigModel.DeviceSources.ToImmutableList();
			} else {
				return ImmutableList<DeviceSourceConfig>.Empty;
			}
		}
		private TomlModelOptions CreateModelOptions() {
			TomlModelOptions modelOptions = new() {
				// ConvertPropertyName = name => TomlNamingHelper2.CamelCaseToPascalCase(name),
				// ConvertPropertyName = name => name,
				ConvertPropertyName = name => {
					//					Console.WriteLine($"Name                  : {name}");
					//					Console.WriteLine($"PascalCaseToCamelCase : {TomlNamingHelper2.PascalCaseToCamelCase(name)}");
					//					Console.WriteLine($"PascalToSnakeCase     : {TomlNamingHelper.PascalToSnakeCase(name)}");
					if(string.IsNullOrEmpty(name)) return name;
					if(String.Equals(name, "DeviceSources")) {
						return "device_source";
					}
					if(String.Equals(name, "DriveSources")) {
						return "drive_source";
					}

					//return TomlNamingHelper2.PascalCaseToCamelCase(name);
					return TomlNamingHelper.PascalToSnakeCase(name);
				},
				//				ConvertFieldName = TomlNamingHelper2.PascalCaseToCamelCase,
				//				ConvertFieldName = Tomlyn.Helpers.TomlNamingHelper.PascalToSnakeCase,
				CreateInstance = MyCreateInstanceImpl
			};
			return modelOptions;
		}
		static object MyCreateInstanceImpl(Type type, ObjectKind kind) {
			if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
				Type inner = type.GetGenericArguments()[0];
				object? obj = Activator.CreateInstance(typeof(List<>).MakeGenericType(inner));
				if(obj != null) {
					return obj;
				} else {
					throw new InvalidOperationException("Null exception");
				}
			}

			if(type == typeof(object)) {
				switch(kind) {
					case ObjectKind.Table:
					case ObjectKind.InlineTable:
						return new TomlTable(kind == ObjectKind.InlineTable);
					case ObjectKind.TableArray:
						return new TomlTableArray();
					default:
						Debug.Assert(kind == ObjectKind.Array);
						return new TomlArray();
				}
			}

			return Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Failed to create an instance of type '{type.FullName}'");
		}

		static string? GetPropertyNameImpl(PropertyInfo prop) {
			string? name = null;
			return name;
		}

		public IBackupSettings? GetBackupSettingsFrom(string? backupConfigFilePath, string? verifyConfigFilePath, string? verifyPath) {
			FileInfo? backupFileInfo = backupConfigFilePath != null ? new FileInfo(backupConfigFilePath) : null;
			FileInfo? verifyFileInfo = verifyConfigFilePath != null ? new FileInfo(verifyConfigFilePath) : null;
			DirectoryInfo? verifyDirInfo = verifyPath != null ? new DirectoryInfo(verifyPath) : null;

			IBackupSettings? backupSettings = default;

			ConfigModel? loadedDeviceConfigModel;
			if(backupFileInfo != null) {
				loadedDeviceConfigModel = backupConfigFilePath != null ? LoadDeviceConfigModel(backupFileInfo) : null;
			} else if(verifyFileInfo != null) {
				loadedDeviceConfigModel = backupConfigFilePath != null ? LoadDeviceConfigModel(verifyFileInfo) : null;
			} else {
				loadedDeviceConfigModel = null;
			}

			if(loadedDeviceConfigModel == null) {
				return null;
			}
			FileInfo? backupConfigFile = backupConfigFilePath != null ? new FileInfo(backupConfigFilePath) : null;


			backupSettings = new BackupSettingsImpl() {
				BackupConfigFile = backupFileInfo,
				VerifyConfigFile = verifyFileInfo,
				VerifyPath = verifyDirInfo,
				DefaultSource = loadedDeviceConfigModel.Default,
				Sources = loadedDeviceConfigModel.DeviceSources
					.Concat<ISourceConfig>(loadedDeviceConfigModel.DriveSources)
					.ToList()
			};

			return backupSettings;
		}
	}
}
