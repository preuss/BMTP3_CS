using BMTP3_CS.BackupSource.PortableDevices;
using BMTP3_CS.Configs;
using BMTP3_CS.CompareFiles;
using BMTP3_CS.Metadata;
using BMTP3_CS.StringVariableSubstitution;
using MediaDevices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ZLogger;

namespace BMTP3_CS.Handlers.Backup {
	[SupportedOSPlatform("windows7.0")]
	internal class BackupHandlerOld {
		public static readonly DateTime MinWin32FileTime = DateTime.FromFileTimeUtc(0);

		public static readonly DateTime startedDateTime = DateTime.Now;

		private static readonly ILogger<BackupHandlerOld> logger = LogManager.GetLogger<BackupHandlerOld>();
		private CancellationToken cancellationToken;

		public BackupHandlerOld(CancellationToken cancellationToken) {
			this.cancellationToken = cancellationToken;
		}

		public void BackupDevices(IList<ConfigDevicePair> devicesToBackup) {
			foreach(ConfigDevicePair pair in devicesToBackup) {
				//Backup(pair.DeviceSourceConfig, pair.MediaDevice);
				BackupDevice(pair.MediaDevice, pair.DeviceSourceConfig);
				if(cancellationToken.IsCancellationRequested) {
					logger.ZLogTrace($"Cancelled whileBackup MediaDevice {pair.MediaDevice.FriendlyName}");
					Console.WriteLine("Backup afbrudt.");
					break;
				}
			}
		}
		public async Task BackupDevicesAsync(IList<ConfigDevicePair> devicesToBackup) {
			foreach(ConfigDevicePair pair in devicesToBackup) {
				await BackupDeviceAsync(pair, cancellationToken);
				if(cancellationToken.IsCancellationRequested) {
					Console.WriteLine("Backup afbrudt.");
					break;
				}
			}
		}

		private void Backup(DeviceSourceConfig config, MediaDevice device) {
			// Implementer backup logikken her
			Console.WriteLine($"Backing up device: {device.FriendlyName}");
		}
		private MediaDirectoryInfo ValidateAndCorrectAndConvertMediaDeviceFolderSourcePathToMediaDirectoryInfo(MediaDevice mediaDevice, string? folderSource) {
			if(string.IsNullOrEmpty(folderSource)) {
				throw new ArgumentNullException($"FolderSource is empty.");
			}
			string correctedFolderSource = folderSource;

			try {
				string rootSource = folderSource.Split(['/', '\\'])[0];
				// We are using this as a way to remove FriendlyName if set in the FolderSource
				// But sometimes the FriendlyName i null og empty.
				string rootSourceFoundName = !string.IsNullOrWhiteSpace(mediaDevice.FriendlyName) ? mediaDevice.FriendlyName :
					!string.IsNullOrWhiteSpace(mediaDevice.Description) ? mediaDevice.Description : mediaDevice.Model;
				if(rootSource.Equals(rootSourceFoundName, StringComparison.Ordinal)
					&& !mediaDevice.DirectoryExists(folderSource)) {
					// Removes rootSource if same as Device Name.
					correctedFolderSource = folderSource.Substring(folderSource.Split(['/', '\\'])[0].Length + 1);
				}
			} catch(COMException e) {
				if(e.Message.Contains("(0x800710D2)")) {
					logger.ZLogError($"The library, drive, or media pool is empty. (0x800710D2)");
					throw new Exception($"The Device '{mediaDevice.FriendlyName}' exist but is empty. Please open and activate physical device.");
				}
			}
			if(!mediaDevice.DirectoryExists(correctedFolderSource)) {
				throw new Exception($"FolderSource '{folderSource}' does not exist on the device.");
			}
			return mediaDevice.GetDirectoryInfo(correctedFolderSource);
		}

		public void BackupDevice(MediaDevice mediaDevice, DeviceSourceConfig deviceSourceConfig) {

		}
		public void BackupDeviceOld(MediaDevice mediaDevice, DeviceSourceConfig deviceSourceConfig) {
			Console.WriteLine("Starting backup of Name: " + mediaDevice.FriendlyName);
			BackupRecordDataStore progressTracker = HandleBackupProgress(mediaDevice, deviceSourceConfig);

			using(MediaDevice currentMediaDevice = mediaDevice) {
				currentMediaDevice.Connect();



				Console.WriteLine("Starting backup of Name: " + currentMediaDevice.FriendlyName);
				if(string.IsNullOrEmpty(deviceSourceConfig.FolderSource)) {
					throw new ArgumentNullException($"FolderSource is empty.");
				}
				try {
					string rootSource = deviceSourceConfig.FolderSource.Split(['/', '\\'])[0];
					// We are using this as a way to remove FriendlyName if set in the FolderSource
					// But sometimes the FriendlyName i null og empty.
					string rootSourceFoundName = !string.IsNullOrWhiteSpace(currentMediaDevice.FriendlyName) ? currentMediaDevice.FriendlyName :
						!string.IsNullOrWhiteSpace(currentMediaDevice.Description) ? currentMediaDevice.Description : currentMediaDevice.Model;
					if(rootSource.Equals(rootSourceFoundName, StringComparison.Ordinal)
						&& !currentMediaDevice.DirectoryExists(deviceSourceConfig.FolderSource)) {
						// Removes rootSource if same as Device Name.
						deviceSourceConfig.FolderSource = deviceSourceConfig.FolderSource.Substring(deviceSourceConfig.FolderSource.Split(['/', '\\'])[0].Length + 1);
					}
					if(!currentMediaDevice.DirectoryExists(deviceSourceConfig.FolderSource)) {
						throw new Exception($"FolderSource {deviceSourceConfig.FolderSource} does not exist on the device.");
					}
				} catch(COMException e) {
					if(e.Message.Contains("(0x800710D2)")) {
						throw new Exception($"The Device '{currentMediaDevice.FriendlyName}' exist but is empty. Please open and activate physical device.");
					}
				}
				string fromPath = deviceSourceConfig.FolderSource;


				if(string.IsNullOrEmpty(deviceSourceConfig.FolderOutput)) {
					throw new ArgumentNullException("FolderOutput er null eller tom.");
				}
				if(deviceSourceConfig.FolderOutput.IndexOfAny(Path.GetInvalidPathChars()) >= 0) {
					throw new Exception(@"FolderOutput Path has invalid chars = {config.FolderSource}");
				}
				if(File.Exists(deviceSourceConfig.FolderSource)) {
					throw new Exception(@"The Directory is a file = {config.FolderSource}");
				}
				string targetDirectoryPath = deviceSourceConfig.FolderOutput;

				CreateDirectoryIfNotExistsAndUpdateTimes(currentMediaDevice.GetDirectoryInfo(fromPath), targetDirectoryPath);

				DirectoryInfo tempDirectoryInfo = new DirectoryInfo(targetDirectoryPath).CreateTempDirectory(CreateTempDirectoryTimeStampPrefix(startedDateTime));
				if(!deviceSourceConfig.HasFilePattern()) {
					BackupFromPath(currentMediaDevice, fromPath, targetDirectoryPath, tempDirectoryInfo);
				} else {
					//if (String.IsNullOrEmpty(config.FilePattern))
					//{
					//    throw new ArgumentNullException("FilePattern er tom.");
					//}
					string filePattern = deviceSourceConfig.FilePattern!;

					//if (String.IsNullOrEmpty(config.FilePatternIfExist))
					//{
					//    throw new ArgumentNullException("FilePatternIfExist er tom.");
					//}
					string filePatternIfExist = deviceSourceConfig.FilePatternIfExist!;

					BackupFromPathWithFilePattern(currentMediaDevice, fromPath, targetDirectoryPath, tempDirectoryInfo, deviceSourceConfig.CompareByBinary ?? true, filePattern, filePatternIfExist);
					DeleteEmptyDirectories(tempDirectoryInfo.FullName);

				}



				/*
				foreach(var pendingFileInfo in fileDictionary.Values) {
					if(pendingFileInfo.IsSaved) {
						logger.ZLogTrace($"File already saved file: {pendingFileInfo.Name}");
						Console.WriteLine($"File already saved file: {pendingFileInfo.Name}");
						continue;
					}
					string file = pendingFileInfo.Path;
					if(cancellationToken.IsCancellationRequested) {
						logger.ZLogInformation($"Backup afbrudt.");
						Console.WriteLine("Backup afbrudt.");
						break;
					}

					try {
						// Kopier filen til temp
						string tempFilePath = Path.Combine("temp", Path.GetFileName(file));
						DownloadFile()
						mediaDevice.DownloadFile(file, tempFilePath);

						// Opret sidecar fil
						string sidecarFilePath = tempFilePath + ".ini";
						CreateSidecarFile(tempFilePath, sidecarFilePath);

						// Flyt filerne til backup folder
						string backupFilePath = Path.Combine("backup", Path.GetFileName(file));
						File.Move(tempFilePath, backupFilePath);
						File.Move(sidecarFilePath, backupFilePath + ".ini");
						pendingFileInfo.IsSaved = true;
					} catch(Exception e) {
						Console.WriteLine($"Fejl under backup af fil {file}: {e.Message}");
					}
					if(cancellationToken.IsCancellationRequested) {
						progressTracker.SaveBackupList();
						cancellationToken.ThrowIfCancellationRequested();
					}
				}
				*/


				currentMediaDevice.Disconnect();
			}
		}

		private BackupRecordDataStore HandleBackupProgress(MediaDevice mediaDevice, DeviceSourceConfig deviceSourceConfig) {
			IList<BackupRecordInfo> fileDictionary = new List<BackupRecordInfo>();
			using(MediaDevice currentMediaDevice = mediaDevice) {
				const bool enableCache = false;
				currentMediaDevice.Connect(MediaDeviceAccess.GenericRead, MediaDeviceShare.Read, enableCache);

				Stopwatch stopwatch = Stopwatch.StartNew();
				//string folderSourceDirectoryPath = deviceSourceConfig.FolderSource ?? throw new ArgumentNullException("FolderSource is empty: " + deviceSourceConfig.FolderSource);
				MediaDirectoryInfo folderSourceDirectoryInfo = ValidateAndCorrectAndConvertMediaDeviceFolderSourcePathToMediaDirectoryInfo(mediaDevice, deviceSourceConfig.FolderSource);
				//MediaDirectoryInfo mediaDirectoryInfo = currentMediaDevice.GetDirectoryInfo(folderSourceDirectoryInfo);
				CreateBackupList(currentMediaDevice, folderSourceDirectoryInfo, fileDictionary);
				stopwatch.Stop();
				Console.WriteLine($"CreateBackupList took {stopwatch.ElapsedMilliseconds} ms");
			}
			BackupRecordDataStore progressTracker;
			FileInfo progressFileInfo;
			if(BackupRecordDataStore.HasDataStore(deviceSourceConfig)) {
				progressTracker = BackupRecordDataStore.LoadDataStore(deviceSourceConfig);
				bool equalsPending = progressTracker.Records.SequenceEqual(fileDictionary);
				if(!equalsPending) {
					logger.ZLogInformation($"You have an updated device, please delete file: {progressTracker.GetDataStoreFileInfo().FullName}");
					throw new Exception("You have an updated device, please delete file: " + progressTracker.GetDataStoreFileInfo().FullName);
				}
			} else {
				progressTracker = new BackupRecordDataStore(deviceSourceConfig, fileDictionary);
				progressFileInfo = progressTracker.SaveDataStore();
			}
			progressTracker = BackupRecordDataStore.LoadDataStore(deviceSourceConfig);

			return progressTracker;
		}


		static string CreateTempDirectoryTimeStampPrefix(DateTime fromDateTime) {
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string tempDirectoryName = $"Temp_{timestamp}_";
			return tempDirectoryName;
		}

		private static void CreateDirectoryIfNotExistsAndUpdateTimes(MediaDirectoryInfo mediaDirectoryInfo, string directoryPath) {
			if(!Directory.Exists(directoryPath)) {
				{
					DirectoryInfo directoryInfo = Directory.CreateDirectory(directoryPath);
					UpdateTimes(mediaDirectoryInfo, directoryInfo);
				}
			}
		}
		private static void UpdateTimes(MediaDirectoryInfo mediaDirectoryInfo, DirectoryInfo directoryInfo) {
			directoryInfo.CreationTime = ValidateWin32FileTime(mediaDirectoryInfo.CreationTime, startedDateTime);
			directoryInfo.LastAccessTime = ValidateWin32FileTime(mediaDirectoryInfo.LastWriteTime, startedDateTime);
			directoryInfo.LastWriteTime = ValidateWin32FileTime(mediaDirectoryInfo.DateAuthored, startedDateTime);
		}
		private static void UpdateTimes(MediaFileInfo mediaDirectoryInfo, FileInfo directoryInfo) {
			directoryInfo.CreationTime = ValidateWin32FileTime(mediaDirectoryInfo.CreationTime, startedDateTime);
			directoryInfo.LastAccessTime = ValidateWin32FileTime(mediaDirectoryInfo.LastWriteTime, startedDateTime);
			directoryInfo.LastWriteTime = ValidateWin32FileTime(mediaDirectoryInfo.DateAuthored, startedDateTime);
		}

		private static DateTime ValidateWin32FileTime(DateTime? dateTimeToValidate, DateTime fallbackDateTime) {
			if(dateTimeToValidate == null) {
				return fallbackDateTime;
			}
			if(dateTimeToValidate.Value >= MinWin32FileTime) {
				return dateTimeToValidate.Value;
			}
			return fallbackDateTime;
		}

		static void BackupFromPath(MediaDevice device, string fromPath, string targetPath, DirectoryInfo tempDirectoryInfo) {
			const bool compareBinary = false;

			// Kopier filer til målmappen
			var files = device.GetFiles(fromPath);
			foreach(string filePath in files) {
				string fileName = Path.GetFileName(filePath);
				string targetFilePath = Path.Combine(targetPath, fileName);
				FileInfo targetTempFileInfo = tempDirectoryInfo.CombineToFileInfo(fileName);

				if(!compareBinary && File.Exists(targetFilePath)) {
					continue;
				}

				MediaFileInfo mediaFileInfo = device.GetFileInfo(filePath);
				double kilobytes = mediaFileInfo.Length / 1024.0;
				try {
					mediaFileInfo.CopyTo(targetTempFileInfo.FullName);
				} catch(COMException) {
					// TODO: Fix retry because if temp file does not exist or is 0 length but media has length fix problem.
					// Why does this has a problem.
					mediaFileInfo.CopyTo(targetTempFileInfo.FullName);
				}
				FileInfo targetFileInfo = new FileInfo(targetFilePath);
				UpdateTimes(mediaFileInfo, targetTempFileInfo);
				//device.DownloadFile(file, targetFilePath);


				string newTargetFilePath = targetFilePath;
				FileInfo newTargetFileInfo = targetFileInfo;
				string newFileName = fileName;
				int counter = 0;
				while(newTargetFileInfo.Exists) {
					//FileComparer fileComparer = new ReadFileInChunksAndCompareAvx2(8 * 1024);
					//FileComparer fileComparer = new Md5Comparer(8 * 1024);
					//FileComparer fileComparer = new ReadFileInChunksAndCompareVector(8 * 1024);
					FileComparer fileComparer = new ReadFileInChunksAndCompareVector(8 * 1024);
					// Files are the same and we do not copy this file.
					if(fileComparer.Compare(targetTempFileInfo.FullName, newTargetFilePath)) {
						// Delete temp file and try next file.
						targetTempFileInfo.Delete();
						goto ContinueOuterLoop;
					}

					counter++;
					newFileName = Path.GetFileNameWithoutExtension(fileName) + "_" + counter + Path.GetExtension(fileName);
					newTargetFilePath = Path.Combine(targetPath, newFileName);
					newTargetFileInfo = new FileInfo(newTargetFilePath);

				}
				Console.WriteLine("FileUsed: " + newTargetFileInfo.FullName);
				FileInfo targetTempSideCarFileInfo = CreateSideCarFile(targetTempFileInfo);
				targetTempFileInfo.MoveTo(newTargetFilePath);
				FileInfo newTargetSideCarPath = newTargetFileInfo.Directory!.CombineToFileInfo(targetTempSideCarFileInfo.Name);
				targetTempSideCarFileInfo.MoveTo(newTargetSideCarPath.FullName);


				Console.WriteLine($"Fil {fileName} kopieret til {targetFilePath}, Size: {kilobytes:F2} KB");

			ContinueOuterLoop:; // To continue to next filePath
			}

			// Rekursivt kopier undermapper
			var directories = device.GetDirectories(fromPath);
			foreach(string directoryPath in directories) {
				// Ignore special star directory or files
				if(directoryPath.EndsWith("*")) continue;

				string dirName = Path.GetFileName(directoryPath);
				string newFromPath = Path.Combine(fromPath, dirName);
				string newTargetPath = Path.Combine(targetPath, dirName);
				CreateDirectoryIfNotExistsAndUpdateTimes(device.GetDirectoryInfo(newFromPath), newTargetPath);

				DirectoryInfo subTempDirectoryInfo = tempDirectoryInfo.CreateSubdirectory(dirName);
				BackupFromPath(device, newFromPath, newTargetPath, subTempDirectoryInfo);
			}
		}

		static FileInfo CreateSideCarFile(FileInfo targetTempFileInfo) {
			string fullName = targetTempFileInfo.FullName;
			string name = targetTempFileInfo.Name;
			//string nameWithoutExtension = Path.GetFileNameWithoutExtension(fullName);
			string extension = ".ini";
			if(targetTempFileInfo.DirectoryName == null) throw new Exception("Something is wrong, I do not know why this directoryName is null: " + targetTempFileInfo.FullName);
			string sideCarFullPath = Path.Combine(targetTempFileInfo.DirectoryName, name + extension);

			using(StreamWriter writer = new StreamWriter(sideCarFullPath)) {
				var metadataFileInfo = AbstractMetadataFileInfo.FromFileInfo(targetTempFileInfo);
				string? sha3_512_str = null;


				using(FileStream stream = File.OpenRead(fullName)) {
					using(SHA512 sha512 = SHA512.Create()) {
						// 0 bytes == "a69f73cca23a9ac5c8b567dc185a756e97c982164fe25859e0d1dcc1475c80a6"
						byte[] hash = sha512.ComputeHash(stream);
						sha3_512_str = BitConverter.ToString(hash).Replace("-", "").ToLower();
					}
				}
				writer.WriteLine("[Settings]");
				writer.WriteLine("OriginalFileName=" + targetTempFileInfo.Name);
				writer.WriteLine("SHA3_512=" + sha3_512_str);
				writer.WriteLine("CreateDateTime=" + targetTempFileInfo.CreationTime.ToUniversalTime().ToString("o"));
				writer.WriteLine("LastAccessDateTime=" + targetTempFileInfo.LastAccessTime.ToUniversalTime().ToString("o"));
				writer.WriteLine("LastWriteDateTime=" + targetTempFileInfo.LastWriteTime.ToUniversalTime().ToString("o"));
				writer.WriteLine("MediaTakenDateTime=" + metadataFileInfo.GetCreatedMediaFileDateTime()?.ToUniversalTime().ToString("o"));
			}
			return new FileInfo(sideCarFullPath);
		}

		static void BackupFromPathWithFilePattern(MediaDevice device, string fromPath, string targetPath, DirectoryInfo tempDirectoryInfo, bool compareByBinary, string filePattern, string filePatternIfExist) {
			// Kopier filer til målmappen
			foreach(string filePath in device.GetFiles(fromPath)) {
				string fileName = Path.GetFileName(filePath);

				MediaFileInfo sourceMediaFileInfo = device.GetFileInfo(filePath);
				double kilobytes = sourceMediaFileInfo.Length / 1024.0;
				FileInfo targetTempFileInfo = DownloadToTempFile(tempDirectoryInfo, sourceMediaFileInfo);

				IMetadataFileInfo metadataFileInfo = new MetadataExtractorFileInfo(targetTempFileInfo);
				DateTime? mediaCreatedDateTime = metadataFileInfo.GetCreatedMediaFileDateTime();
				DateTime fileCreatedDateTime = File.GetCreationTime(targetTempFileInfo.FullName);
				//DateTime fileCreationTime = targetTempFileInfo.CreationTime;

				int counter = 0;
				Template template = new Template();
				{
					// TODO: Later we should be able to add config variable to choose if using pattern for files without media created datetime.
					DateTime fileCreationTime = mediaCreatedDateTime.GetValueOrDefault(fileCreatedDateTime);

					string year4 = fileCreationTime.ToString("yyyy");
					string month2 = fileCreationTime.ToString("MM");
					string day2 = fileCreationTime.ToString("dd");
					string hour24 = fileCreationTime.ToString("dd");
					string minutes2 = fileCreationTime.ToString("mm");
					string seconds2 = fileCreationTime.ToString("ss");
					string milliseconds3 = fileCreationTime.ToString("fff");
					string originalName = Path.GetFileNameWithoutExtension(filePath);
					string extension = Path.GetExtension(filePath).TrimStart('.');

					template.AddVariable("yyyy", year4);
					template.AddVariable("MM", month2);
					template.AddVariable("dd", day2);
					template.AddVariable("HH", hour24);
					template.AddVariable("mm", minutes2);
					template.AddVariable("ss", seconds2);
					template.AddVariable("fff", milliseconds3);
					template.AddVariable("originalName", originalName);
					template.AddVariable("ext", extension);
					template.AddVariable("count", counter);
				}

				string filePatternTargetFilePath = Path.Combine(targetPath, filePattern);
				string filePatternIfExistTargetFilePath = Path.Combine(targetPath, filePatternIfExist);

				string newTargetFilePath = template.Replace(filePatternTargetFilePath);
				FileInfo newTargetFileInfo = new FileInfo(newTargetFilePath);

				if(newTargetFileInfo.Exists) {
					//FileComparer fileComparer = new ReadFileInChunksAndCompareAvx2(8 * 1024);
					//FileComparer fileComparer = new Md5Comparer(8 * 1024);
					//FileComparer fileComparer = new ReadFileInChunksAndCompareVector(8 * 1024);
					FileComparer fileComparer = new ReadFileInChunksAndCompareVector(8 * 1024);
					// Files are the same and we do not copy this file.
					if(fileComparer.Compare(targetTempFileInfo.FullName, newTargetFilePath)) {
						// Delete temp file and try next file.
						targetTempFileInfo.Delete();
						goto ContinueOuterLoop;
					}
					while(true) {
						counter++;

						template.AddVariable("count", counter);

						string testNextCountPath = template.Replace(filePatternIfExistTargetFilePath);
						if(!File.Exists(testNextCountPath)) {
							newTargetFilePath = template.Replace(filePatternIfExistTargetFilePath);
							newTargetFileInfo = new FileInfo(newTargetFilePath);
							break;
						}

						fileComparer = new ReadFileInChunksAndCompareVector(8 * 1024);
						// Files are the same and we do not copy this file.
						if(fileComparer.Compare(targetTempFileInfo.FullName, newTargetFilePath)) {
							// Delete temp file and try next file.
							targetTempFileInfo.Delete();
							goto ContinueOuterLoop;
						}

					}
				}
				if(!Directory.Exists(newTargetFileInfo.DirectoryName)) {
					Directory.CreateDirectory(newTargetFileInfo.DirectoryName!);
				}

				Console.WriteLine("FileUsed: " + newTargetFileInfo.FullName);
				targetTempFileInfo.MoveTo(newTargetFilePath);

				Console.WriteLine($"Fil {fileName} kopieret til {newTargetFilePath}, Size: {kilobytes:F2} KB");

			ContinueOuterLoop:; // To continue to next filePath
			}

			// Rekursivt kopier undermapper
			var directories = device.GetDirectories(fromPath);
			foreach(string directoryPath in directories) {
				string dirName = Path.GetFileName(directoryPath);
				string newFromPath = Path.Combine(fromPath, dirName);
				string newTargetPath = Path.Combine(targetPath, dirName);


				//CreateDirectoryIfNotExistsAndUpdateTimes(device.GetDirectoryInfo(newFromPath), newTargetPath);

				DirectoryInfo subTempDirectoryInfo = tempDirectoryInfo.CreateSubdirectory(dirName);
				BackupFromPathWithFilePattern(device, newFromPath, targetPath, subTempDirectoryInfo, compareByBinary, filePattern, filePatternIfExist);
			}
		}


		static FileInfo DownloadToTempFile(DirectoryInfo tempDirectoryInfo, MediaFileInfo sourceMediaFileInfo) {
			string fileName = sourceMediaFileInfo.Name;
			FileInfo targetTempFileInfo = tempDirectoryInfo.CombineToFileInfo(fileName);

			sourceMediaFileInfo.CopyTo(targetTempFileInfo.FullName);
			UpdateTimes(sourceMediaFileInfo, targetTempFileInfo);

			return targetTempFileInfo;
		}

		static bool DeleteEmptyDirectories(string path) {
			bool isEmpty = true;

			foreach(string dir in Directory.GetDirectories(path)) {
				if(!DeleteEmptyDirectories(dir)) {
					isEmpty = false;
				}
			}

			if(isEmpty && Directory.GetFiles(path).Length == 0) {
				Directory.Delete(path);
				return true;
			}
			return false;
		}


		private static void DownloadFile(DeviceSourceConfig config, MediaDevice device, BackupRecordDataStore progressTracker) {
			using(device) {
				device.Connect();

				Console.WriteLine("Starting file download from device: " + device.FriendlyName);
				if(string.IsNullOrEmpty(config.FolderSource)) {
					throw new ArgumentNullException($"FolderSource is empty.");
				}

				if(!device.DirectoryExists(config.FolderSource)) {
					throw new Exception($"FolderSource {config.FolderSource} does not exist on the device.");
				}

				// Assuming you have a specific file name to download
				string fileName = "example.txt"; // Replace with the actual file name
				string sourceFilePath = Path.Combine(config.FolderSource, fileName);

				if(!device.FileExists(sourceFilePath)) {
					throw new Exception($"File {sourceFilePath} does not exist on the device.");
				}

				// Assuming you have a local folder where you want to save the downloaded file
				string localFolderPath = @"C:\MyBackupFolder"; // Replace with your desired local folder path
				string localFilePath = Path.Combine(localFolderPath, fileName);

				// Download the file
				device.DownloadFile(sourceFilePath, localFilePath);

				Console.WriteLine($"File {fileName} downloaded successfully to {localFilePath}.");

				device.Disconnect();
			}
		}
		public async Task BackupDeviceAsync(ConfigDevicePair pair, CancellationToken cancellationToken) {
			//IEnumerable<string> fileList = new List<string>();
			//IEnumerable<string> fileList = new ConcurrentBag<string>();
			ConcurrentQueue<BackupRecordInfo> fileDictionary = new ConcurrentQueue<BackupRecordInfo>();


			using(MediaDevice currentMediaDevice = pair.MediaDevice) {
				const bool enableCache = false;
				currentMediaDevice.Connect(MediaDeviceAccess.GenericRead, MediaDeviceShare.Read, enableCache);

				var stopwatch = Stopwatch.StartNew();
				try {
					//await CreateBackupListAsync(pair.MediaDevice, "/", fileList, cancellationToken);
					await CreateBackupListAsyncParallel(pair.MediaDevice, "/", fileDictionary, cancellationToken);
				} catch(OperationCanceledException) {
					throw new BackupCanceledException("Creating backup list", cancellationToken);
				} finally {

					//TODO: Remove stopwatch after test of speed.
					stopwatch.Stop();
					Console.WriteLine($"CreateBackupList took {stopwatch.ElapsedMilliseconds} ms");
				}
			}
			BackupRecordDataStore progressTracker = new BackupRecordDataStore(pair.DeviceSourceConfig, fileDictionary.ToList());
			FileInfo progressFileInfo = progressTracker.SaveDataStore();

			foreach(var pendingFileInfo in fileDictionary) {
				if(cancellationToken.IsCancellationRequested) {
					Console.WriteLine("Backup afbrudt.");
					break;
				}

				try {
					// Kopier filen til temp
					string tempFilePath = Path.Combine("temp", Path.GetFileName(pendingFileInfo.Path));
					pair.MediaDevice.DownloadFile(pendingFileInfo.Path, tempFilePath);

					// Opret sidecar fil
					string sidecarFilePath = tempFilePath + ".ini";
					CreateSidecarFile(tempFilePath, sidecarFilePath);

					// Flyt filerne til backup folder
					string backupFilePath = Path.Combine("backup", Path.GetFileName(pendingFileInfo.Path));
					File.Move(tempFilePath, backupFilePath);
					File.Move(sidecarFilePath, backupFilePath + ".ini");
				} catch(Exception e) {
					Console.WriteLine($"Fejl under backup af fil {pendingFileInfo.Path}: {e.Message}");
				}
			}
		}

		public void CreateSidecarFile(string filePath, string sidecarFilePath) {
			try {
				// Beregn SHA3 hash af filen
				string sha3Hash = ComputeSHA3Hash(filePath);

				// Få filens sidste ændringsdato
				DateTime lastModified = File.GetLastWriteTime(filePath);

				// Opret sidecar filens indhold
				StringBuilder sidecarContent = new StringBuilder();
				sidecarContent.AppendLine($"File: {Path.GetFileName(filePath)}");
				sidecarContent.AppendLine($"SHA3 Hash: {sha3Hash}");
				sidecarContent.AppendLine($"Last Modified: {lastModified}");

				// Skriv sidecar filen
				File.WriteAllText(sidecarFilePath, sidecarContent.ToString());
			} catch(Exception e) {
				Console.WriteLine($"Fejl under oprettelse af sidecar fil for {filePath}: {e.Message}");
			}
		}

		private string ComputeSHA3Hash(string filePath) {
			using(var sha3 = SHA512.Create()) {
				using(var stream = File.OpenRead(filePath)) {
					byte[] hashBytes = sha3.ComputeHash(stream);
					return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
				}
			}
		}

		public void CreateBackupList(MediaDevice mediaDevice, MediaDirectoryInfo rootPath, IList<BackupRecordInfo> backupRecords) {
			if(cancellationToken.IsCancellationRequested) {
				throw new OperationCanceledException("CreateBackupList", cancellationToken);
			}

			foreach(MediaFileInfo mediaFileInfo in rootPath.EnumerateFiles()) {
				BackupRecordInfo pendingFileInfo = new BackupRecordInfo(
					persistentUniqueId: mediaFileInfo.PersistentUniqueId,
					path: mediaFileInfo.FullName,
					size: mediaFileInfo.Length
				) {
					Name = mediaFileInfo.Name,
					DateCreated = mediaFileInfo.CreationTime,
					DateModified = mediaFileInfo.LastWriteTime,
					DateAuthored = mediaFileInfo.DateAuthored
				};
				backupRecords.Add(pendingFileInfo);
			}
			foreach(MediaDirectoryInfo mediaDirectoryInfo in rootPath.EnumerateDirectories()) {
				CreateBackupList(mediaDevice, mediaDirectoryInfo, backupRecords);
			}
		}

		// Opret backup liste parallelt
		public void CreateBackupListParallel(MediaDevice mediaDevice, string rootPath, List<string> fileList) {
			var files = mediaDevice.EnumerateFiles(rootPath).ToList();
			var directories = mediaDevice.EnumerateDirectories(rootPath).ToList();

			fileList.AddRange(files);

			Parallel.ForEach(directories, directory => {
				CreateBackupListParallel(mediaDevice, directory, fileList);
			});
		}
		public async Task CreateBackupListAsync(MediaDevice mediaDevice, string rootPath, List<string> fileList, CancellationToken cancellationToken) {
			var files = await Task.Run(() => mediaDevice.EnumerateFiles(rootPath).ToList(), cancellationToken);
			var directories = await Task.Run(() => mediaDevice.EnumerateDirectories(rootPath).ToList(), cancellationToken);

			fileList.AddRange(files);

			var tasks = directories.Select(directory => CreateBackupListAsync(mediaDevice, directory, fileList, cancellationToken));
			await Task.WhenAll(tasks);
		}

		public async Task CreateBackupListAsyncParallel(MediaDevice mediaDevice, string rootPath, ConcurrentQueue<BackupRecordInfo> fileDictionary, CancellationToken cancellationToken) {
			var files = await Task.Run(() => mediaDevice.EnumerateFiles(rootPath).ToList(), cancellationToken);
			var directories = await Task.Run(() => mediaDevice.EnumerateDirectories(rootPath).ToList(), cancellationToken);

			foreach(var file in files) {
				MediaFileInfo mediaFileInfo = mediaDevice.GetFileInfo(file);
				BackupRecordInfo pendingFileInfo = new BackupRecordInfo(
					persistentUniqueId: mediaFileInfo.PersistentUniqueId,
					path: mediaFileInfo.FullName,
					size: mediaFileInfo.Length
				) {
					Name = mediaFileInfo.Name,
					DateCreated = mediaFileInfo.CreationTime,
					DateModified = mediaFileInfo.LastWriteTime,
					DateAuthored = mediaFileInfo.DateAuthored
				};
				fileDictionary.Enqueue(pendingFileInfo);
			}

			var tasks = directories.Select(directory => Task.Run(() => CreateBackupListAsyncParallel(mediaDevice, directory, fileDictionary, cancellationToken), cancellationToken));
			await Task.WhenAll(tasks);
		}

	}

}
