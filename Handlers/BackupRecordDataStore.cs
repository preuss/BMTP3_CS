using BMTP3_CS.BackupSource;
using BMTP3_CS.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Handlers {
	internal class BackupRecordDataStore {
		private readonly ISourceConfig sourceConfig;
		private readonly IList<BackupRecordInfo> records;
		public BackupRecordDataStore(ISourceConfig sourceConfig, IList<BackupRecordInfo> records) {
			this.sourceConfig = sourceConfig;
			this.records = records;
		}
		public void AddRecord(BackupRecordInfo record) {
			records.Add(record);
		}
		public ISourceConfig SourceConfig { get { return sourceConfig; } }
		public IList<BackupRecordInfo> Records { get { return records; } }
		private static string GetFileNameFrom(ISourceConfig config) {
			string fileName = $"Progress_{config.Title}_{config.Name}.json";
			foreach(char invalidChar in Path.GetInvalidFileNameChars()) {
				fileName = fileName.Replace(invalidChar.ToString(), string.Empty);
			}
			return fileName;
		}
		private static FileInfo GetDataStoreFileInfoUsing(ISourceConfig sourceConfig) {
			string targetPath = sourceConfig.FolderOutput!;
			string fileName = GetFileNameFrom(sourceConfig);
			string fullPath = Path.Combine(targetPath, fileName);
			return new FileInfo(fullPath);
		}
		public FileInfo GetDataStoreFileInfo() {
			return GetDataStoreFileInfoUsing(sourceConfig);
		}
		public FileInfo SaveDataStore() {
			FileInfo dataStoreFileInfo = GetDataStoreFileInfo();
			JsonConverter[] converters = new JsonConverter[] { new SourceTypeConverter(), new SourceConfigConverter() };
			string json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, converters);
			File.WriteAllText(dataStoreFileInfo.FullName, json, Encoding.UTF8);
			Console.WriteLine($"Fuld sti til filen: {dataStoreFileInfo.FullName}");
			return dataStoreFileInfo;
		}
		public static BackupRecordDataStore LoadDataStore(ISourceConfig sourceConfig) {
			FileInfo dataStoreFileInfo = GetDataStoreFileInfoUsing(sourceConfig);

			if(!HasDataStore(sourceConfig)) {
				throw new FileNotFoundException("The specified file does not exist.", dataStoreFileInfo.FullName);
			}

			string jsonData = File.ReadAllText(dataStoreFileInfo.FullName);

			JsonConverter[] converters = new JsonConverter[] { new SourceTypeConverter(), new SourceConfigConverter() };
			return JsonConvert.DeserializeObject<BackupRecordDataStore>(jsonData, converters) ?? throw new NullReferenceException("Problem with readin json file: " + dataStoreFileInfo.FullName);
		}
		public static BackupRecordDataStore LoadDataOrCreateDataStore(string fileName) {
			throw new NotImplementedException();
		}
		internal static bool HasDataStore(ISourceConfig sourceConfig) {
			return GetDataStoreFileInfoUsing(sourceConfig).Exists;
		}
	}
	public class SourceTypeConverter : JsonConverter<SourceType> {
		public override SourceType ReadJson(JsonReader reader, Type objectType, SourceType existingValue, bool hasExistingValue, JsonSerializer serializer) {
			JToken token = JToken.Load(reader);
			if(token.Type == JTokenType.String && token.Value<string>() is string value) {
				return (SourceType)Enum.Parse(typeof(SourceType), value);
			} else {
				throw new Exception("Invalid JSON value for SourceType");
			}
		}
		public override void WriteJson(JsonWriter writer, SourceType value, JsonSerializer serializer) {
			// Not really used
			// TODO: Find out why not used, and why this code has to be added to SourceConfigConverter.WriteJson
			// TODO: Find out how to use this in SourceConfigConverter
			writer.WriteValue(value.ToString());
		}
	}
	public class SourceConfigConverter : JsonConverter<ISourceConfig?> {
		public override ISourceConfig? ReadJson(JsonReader reader, Type objectType, ISourceConfig? existingValue, bool hasExistingValue, JsonSerializer serializer) {
			JObject jo = JObject.Load(reader);
			SourceType? sourceType = jo["SourceType"]?.ToObject<SourceType>(serializer); // Uses SourceTypeConverter for ReadJson

			if(sourceType == null) {
				throw new ArgumentException("Invalid source type", nameof(sourceType));
			}

			switch(sourceType) {
				case SourceType.Drive:
					return jo.ToObject<DriveSourceConfig>();
				case SourceType.Device:
					return jo.ToObject<DeviceSourceConfig>();
				default:
					throw new ArgumentException("Invalid source type", nameof(sourceType));
			}
		}
		public override void WriteJson(JsonWriter writer, ISourceConfig? value, JsonSerializer serializer) {
			if(value == null) {
				// Handle the case when value is null
				// For example, you can throw an exception or write a default value
				throw new ArgumentNullException(nameof(value));
			}

			JObject jo = JObject.FromObject(value);

			// Check if the property already exists
			jo.Remove("SourceType");
			if(!jo.ContainsKey("SourceType")) {
				jo.Add("SourceType", JToken.FromObject(value.SourceType.ToString()));
			}

			jo.WriteTo(writer);
		}
	}
}
