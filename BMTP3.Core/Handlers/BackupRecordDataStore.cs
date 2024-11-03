using BMTP3.Core.BackupSource;
using BMTP3.Core.Configs;
using MediaDevices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Handlers {
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
			string json = JsonConvert.SerializeObject(this, Formatting.Indented, converters);
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
		public static BackupRecordDataStore LoadDataOrCreateDataStore(ISourceConfig sourceConfig, IList<BackupRecordInfo> backupRecords) {
			FileInfo dataStoreFileInfo = GetDataStoreFileInfoUsing(sourceConfig);
			if(HasDataStore(sourceConfig)) {
				return LoadDataStore(sourceConfig);
			}
			return new BackupRecordDataStore(sourceConfig, backupRecords);
		}
		internal static bool HasDataStore(ISourceConfig sourceConfig) {
			return GetDataStoreFileInfoUsing(sourceConfig).Exists;
		}
	}
}
