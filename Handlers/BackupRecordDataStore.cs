using BMTP3_CS.Configs;
using Newtonsoft.Json;
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
			string json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);

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

			return JsonConvert.DeserializeObject<BackupRecordDataStore>(jsonData) ?? throw new NullReferenceException("Problem with readin json file: " + fileName);
		}
		internal static bool HasDataStore(ISourceConfig sourceConfig) {
			return Path.Exists(GetFileNameFrom(sourceConfig));
		}
	}
}
