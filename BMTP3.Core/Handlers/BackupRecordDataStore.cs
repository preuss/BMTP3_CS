using BMTP3.Core.Configs;
using Newtonsoft.Json;
using System.Text;

namespace BMTP3.Core.Handlers {
	/// <summary>
	/// This class is used to store backup records.
	/// // TODO: I amd thinkging of refactor rename this to BackupRecordDataRespository, or to keep Store I do not know
	/// </summary>
	internal class BackupRecordDataStore {
		private readonly BackupRecordDataStorePathResolver _pathResolver;
		private readonly BackupRecordData _recordData;
		public BackupRecordDataStore(ISourceConfig sourceConfig, IList<BackupRecordInfo> records) {
			_pathResolver = new BackupRecordDataStorePathResolver(sourceConfig);
			_recordData = new BackupRecordData(sourceConfig, records);
		}
		public void AddRecord(BackupRecordInfo record) {
			_recordData.Records.Add(record);
		}
		public ISourceConfig SourceConfig => _recordData.SourceConfig;
		public IList<BackupRecordInfo> Records => _recordData.Records;
		public FileInfo GetDataStoreFileInfo() => _pathResolver.Resolve();
		public FileInfo SaveDataStore() {
			FileInfo dataStoreFileInfo = GetDataStoreFileInfo();
			JsonConverter[] converters = new JsonConverter[] { new SourceTypeConverter(), new SourceConfigConverter() };
			string json = JsonConvert.SerializeObject(_recordData, Formatting.Indented, converters);
			File.WriteAllText(dataStoreFileInfo.FullName, json, Encoding.UTF8);
			Console.WriteLine($"Fuld sti til filen: {dataStoreFileInfo.FullName}");
			return dataStoreFileInfo;
		}
		public static BackupRecordDataStore LoadDataStore(ISourceConfig sourceConfig) {
			FileInfo dataStoreFileInfo = new BackupRecordDataStorePathResolver(sourceConfig).Resolve();

			if(!HasDataStore(sourceConfig)) {
				throw new FileNotFoundException("The specified file does not exist.", dataStoreFileInfo.FullName);
			}

			string jsonData = File.ReadAllText(dataStoreFileInfo.FullName);

			JsonConverter[] converters = new JsonConverter[] { new SourceTypeConverter(), new SourceConfigConverter() };
			BackupRecordData recordData = JsonConvert.DeserializeObject<BackupRecordData>(jsonData, converters) ?? throw new NullReferenceException("Problem with readin json file: " + dataStoreFileInfo.FullName);

			return new BackupRecordDataStore(recordData.SourceConfig, recordData.Records);
		}
		public static BackupRecordDataStore LoadDataOrCreateDataStore(ISourceConfig sourceConfig, IList<BackupRecordInfo> backupRecords) {
			return HasDataStore(sourceConfig)
				? LoadDataStore(sourceConfig)
				: new BackupRecordDataStore(sourceConfig, backupRecords);
		}
		internal static bool HasDataStore(ISourceConfig sourceConfig) {
			return new BackupRecordDataStorePathResolver(sourceConfig).Resolve().Exists;
		}
	}
}
