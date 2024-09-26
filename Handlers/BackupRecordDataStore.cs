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
		public void AddRecord(string uniqueFileId, BackupRecordInfo record) {
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
		public FileInfo GetDataStoreFileInfo() {
			return new FileInfo(GetFileNameFrom(sourceConfig));
		}
		public FileInfo SaveDataStore() {
			string fileName = GetFileNameFrom(sourceConfig);
			string json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, new SourceTypeConverter());

			File.WriteAllText(fileName, json, Encoding.UTF8);

			string fullPath = Path.GetFullPath(fileName);
			Console.WriteLine($"Fuld sti til filen: {fullPath}");
			return new FileInfo(fullPath);
		}
		public static BackupRecordDataStore LoadDataStore(ISourceConfig sourceConfig) {
			string fileName = GetFileNameFrom(sourceConfig);

			if(!HasDataStore(sourceConfig)) {
				throw new FileNotFoundException("The specified file does not exist.", fileName);
			}

			string jsonData = File.ReadAllText(fileName);

			JsonConverter[] converters = new JsonConverter[] { new SourceTypeConverter(), new SourceConfigConverter() };
			return JsonConvert.DeserializeObject<BackupRecordDataStore>(jsonData, converters) ?? throw new NullReferenceException("Problem with readin json file: " + fileName);
		}
		internal static bool HasDataStore(ISourceConfig sourceConfig) {
			return Path.Exists(GetFileNameFrom(sourceConfig));
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
			writer.WriteValue(value.ToString());
		}
	}
	public class SourceConfigConverter : JsonConverter<ISourceConfig> {
		public override ISourceConfig? ReadJson(JsonReader reader, Type objectType, ISourceConfig? existingValue, bool hasExistingValue, JsonSerializer serializer) {
			JObject jo = JObject.Load(reader);
			SourceType? sourceType = jo["SourceType"]?.ToObject<SourceType>();

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
			jo.Add("SourceType", JToken.FromObject(value.GetType()));
			jo.WriteTo(writer);
		}
	}
}
