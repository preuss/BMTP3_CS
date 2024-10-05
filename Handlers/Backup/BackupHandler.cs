using BMTP3_CS.Configs;
using MediaDevices;
using MediaDevices.Progress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Xml;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ZLogger;
using BMTP3_CS.CompareFiles;
using BMTP3_CS.Metadata;
using BMTP3_CS.StringVariableSubstitution;
using Newtonsoft.Json.Linq;
using System.Collections.Frozen;
using Spectre.Console;
using BMTP3_CS.Consoles.Progress.Columns;
using BMTP3_CS.Consoles.Spinner;
using BMTP3_CS.Consoles.Progress;
using BMTP3_CS.Consoles.ProgressStatus;
using Blake3;
using System.Threading.Tasks.Sources;
using SHA3.Net;
using static BMTP3_CS.Handlers.HashCalculator.HashType;
using BMTP3_CS.BackupSource.PortableDevices;
using System.IO;

namespace BMTP3_CS.Handlers.Backup {
	[SupportedOSPlatform("windows7.0")]
	internal class BackupHandler {
		public static readonly DateTime MinWin32FileTime = DateTime.MinValue;

		public static readonly DateTime startedDateTime = DateTime.Now;

		private static readonly ILogger<BackupHandler> logger = LogManager.GetLogger<BackupHandler>();

		private readonly IAnsiConsole Console;

		private readonly CancellationTokenGenerator _cancellationTokenGenerator;
		private readonly CancellationToken _cancellationToken;
		private readonly BackupHelper _backupHelper;

		private readonly FileComparer _fileComparer;

		private BackupHelper BackupHelper { get { return _backupHelper; } }

		public BackupHandler(IAnsiConsole console, CancellationTokenGenerator cancellationTokenGenerator, BackupHelper backupHelper, FileComparer fileComparer) {
			Console = console;
			_cancellationTokenGenerator = cancellationTokenGenerator;
			_cancellationToken = cancellationTokenGenerator.Token;
			_backupHelper = backupHelper;

			_fileComparer = fileComparer;
		}

		Action<int> CreateIncrementCallback(Action<int> updateAction) {
			int totalCount = 0;
			return count => {
				totalCount += count;
				updateAction(totalCount);
			};
		}

		public void BackupDevice(MediaDevice device, DeviceSourceConfig config, DateTime backupStartDateTime) {
			Console.WriteLine($"Backing up device: {device.FriendlyName}");

			List<BackupRecordInfo> allDeviceFiles = new List<BackupRecordInfo>();
			IDictionary<string, MediaFileInfo> uniqueIdMediaFileInfos;
			using(device) {
				device.Connect();

				MediaDirectoryInfo folderSourceDirectoryInfo = ValidateAndCorrectFolderSourcePathToMediaDirectoryInfo(device, config.FolderSource);

				Stopwatch stopwatch = Stopwatch.StartNew();
				//IList<MediaFileInfo> allMediaInfoFiles = await ReadAllFilesAsync(folderSourceDirectoryInfo);
				/*
				IList<MediaFileInfo> allMediaInfoFiles = AnsiConsole.Status().Spinner(new SequenceSpinner(SequenceSpinner.Sequence6))
					.Start("Indlæser filer og biblioteker fra enhed...", ctx => {
						return ReadAllFiles(folderSourceDirectoryInfo, new FileAndDirectoryCounter(ctx));
					});
				*/
				IList<MediaFileInfo> allMediaInfoFiles = new List<MediaFileInfo>();
				AnsiConsole.Progress()
					.AutoRefresh(true)
					.Columns(new ProgressColumn[] {
						new SpinnerColumn(new SequenceSpinner(SequenceSpinner.Sequence6)),
						new TaskDescriptionColumn(),
						new CounterColumn<int>("Count") { CounterStyle = new Style(decoration: Decoration.Bold) },
						new ElapsedTimeAdvancedColumn(),
					})
					.Start(ctx => {
						//var countingFilesAndDirTask = ctx.AddTask("Counting Files and Dirs");
						//countingFilesAndDirTask.IsIndeterminate = true;

						ProgressTask countingFilesTask = ctx.AddTask("Counting Files");
						countingFilesTask.IsIndeterminate = true;

						ProgressTask countingDirsTask = ctx.AddTask("Counting Dirs");
						countingDirsTask.IsIndeterminate = true;

						CancellationToken cancellationToken = _cancellationTokenGenerator.Token;

						if(cancellationToken.IsCancellationRequested) {
							cancellationToken.ThrowIfCancellationRequested();
						}


						Action<int> fileIncrementCallback = CreateIncrementCallback(count => { countingFilesTask.State.Update<int>("Count", _ => count); });
						Action<int> dirIncrementCallback = CreateIncrementCallback(count => { countingDirsTask.State.Update<int>("Count", _ => count); });
						FileAndDirectoryCounter progressReporter = new FileAndDirectoryCounter(fileIncrementCallback: fileIncrementCallback, dirIncrementCallback: dirIncrementCallback);
						//allMediaInfoFiles = ReadAllFiles(folderSourceDirectoryInfo, new FileAndDirectoryCounter(countingFilesAndDirTask), cancellationToken);
						//allMediaInfoFiles = ReadAllFiles(folderSourceDirectoryInfo, progressReporter, cancellationToken);
						allMediaInfoFiles = ReadAllFiles(folderSourceDirectoryInfo, fileIncrementCallback, dirIncrementCallback, cancellationToken);
					});
				//IList<MediaFileInfo> allMediaInfoFiles = ReadAllFiles(folderSourceDirectoryInfo);
				stopwatch.Stop();
				Console.WriteLine($"CreateBackupList took {stopwatch.ElapsedMilliseconds} ms");

				uniqueIdMediaFileInfos = allMediaInfoFiles.ToFrozenDictionary(mediaFileInfo => mediaFileInfo.PersistentUniqueId, mediaFileInfo => mediaFileInfo);

				foreach(var mediaFileInfo in allMediaInfoFiles) {
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
					pendingFileInfo.UpdateGeneratedId(); // TODO: Calculated another place.
					allDeviceFiles.Add(pendingFileInfo);
				}
				device.Disconnect();
			}
			// Sort for better backup progress.
			allDeviceFiles.Sort((a, b) => string.Compare(a.Path, b.Path));
			BackupRecordDataStore backupProgressTracker = LoadProgress(config, allDeviceFiles, exceptionIfChanged: true);

			try {
				using(device) {
					device.Connect();

					MediaDirectoryInfo sourceDirectoryInfo = ValidateAndCorrectFolderSourcePathToMediaDirectoryInfo(device, config.FolderSource);

					// Validate FolderOutput, where the output of files should be.
					if(string.IsNullOrEmpty(config.FolderOutput)) {
						throw new ArgumentNullException(nameof(config.FolderOutput), "FolderOutput er null eller tom.");
					}
					if(config.FolderOutput.IndexOfAny(Path.GetInvalidPathChars()) >= 0) {
						throw new ArgumentException($"FolderOutput Path has invalid chars = '{config.FolderSource}'.", nameof(config.FolderSource));
					}
					if(File.Exists(config.FolderSource)) {
						throw new ArgumentException($"The Directory is a file = {config.FolderSource}", nameof(config.FolderSource));
					}
					DirectoryInfo targetDirectoryInfo = new DirectoryInfo(config.FolderOutput);
					if(!targetDirectoryInfo.Exists) {
						targetDirectoryInfo.Create();
						// Update DateTimes for directory as the same as source folder.
						BackupHelper.UpdateDateTimeForDirectoryInfo(sourceDirectoryInfo, targetDirectoryInfo);
					}
					DirectoryInfo tempDirectoryInfo = targetDirectoryInfo.CreateTempDirectory(BackupHelper.CreateTempDirectoryTimeStampPrefix(startedDateTime));

					Console.WriteLine();
					AnsiConsole.Progress()
						.AutoClear(true)
						.AutoRefresh(true)
						.HideCompleted(true)
						.Columns(new ProgressColumn[] {
							new SpinnerColumn(new SequenceSpinner(SequenceSpinner.Sequence7)),         // Spinner
							new TaskDescriptionColumn(), // Beskrivelse af opgaven
							new ProgressBarColumn() {Width=10},     // Fremdriftsbjælke
							new PercentageColumn(),      // Procentdel
							new RemainingTimeColumn(),   // Resterende tid
							new ValueOfMaxColumn(),
						})
						.Start(ctx => {
							var overallTask = ctx.AddTask("[green]Total Progress[/]", new ProgressTaskSettings { AutoStart = true, MaxValue = backupProgressTracker.Records.Count });
							int countFiles = 0;

							List<BackupRecordInfo> pendingFileInfos = backupProgressTracker.Records.ToList();
							pendingFileInfos.Sort((a, b) => string.Compare(a.Path, b.Path));
							foreach(BackupRecordInfo pendingFileInfo in pendingFileInfos) {
								countFiles++;
								overallTask.Description = $"[green]Total Progress[/]";
								if(_cancellationToken.IsCancellationRequested) {
									Console.WriteLine("Backup afbrudt.");
									_cancellationToken.ThrowIfCancellationRequested();
								}

								if(pendingFileInfo.IsSaved) {
									overallTask.Increment(1);
									continue;
								}

								if(!uniqueIdMediaFileInfos.TryGetValue(pendingFileInfo.PersistentUniqueId, out var sourceMediaFileInfo)) {
									Console.WriteLine($"Fil ikke fundet: {pendingFileInfo.PersistentUniqueId}");
									overallTask.Increment(1);
									continue;
								}
								//var downloadFileTask = ctx.AddTask($"[green]Downloading file: [/][white]{sourceMediaFileInfo.FullName}[/]", new ProgressTaskSettings { AutoStart = true, MaxValue = sourceMediaFileInfo.Length });
								var downloadFileTask = ctx.AddTask($"[green]Downloading file: [/][white]{BackupHelper.ShortenPath(sourceMediaFileInfo.FullName, 27)}[/]", new ProgressTaskSettings { AutoStart = true, MaxValue = sourceMediaFileInfo.Length });
								//var downloadFileTask = ctx.AddTask($"[green]Downloading file: [/][white]{sourceMediaFileInfo.Name}[/]", new ProgressTaskSettings { AutoStart = true, MaxValue = sourceMediaFileInfo.Length });
								overallTask.Description = $"[green]Total Progress[/] - Downloading File: {sourceMediaFileInfo.Name}";
								Progress<FileProgressReport> fileProgress = new Progress<FileProgressReport>();
								fileProgress.ProgressChanged += (sender, report) => {
									downloadFileTask.Value(report.BytesRead);
								};

								bool isSaved;
								if(!config.HasFilePattern()) {
									//throw new InvalidOperationException("Har ikke File Pattern, og mangler at implementere BackupFromPath(device, fromPath, targetDirectoryPath, tempDirectoryInfo);");
									const bool addSideCarFile = false;
									isSaved = BackupFromPath(backupStartDateTime, device, sourceMediaFileInfo, targetDirectoryInfo, tempDirectoryInfo, config.CompareByBinary ?? true, addSideCarFile, fileProgress);
								} else {
									string filePattern = config.FilePattern!;
									string filePatternIfExist = config.FilePatternIfExist!;

									isSaved = BackupFromPathWithFilePattern(backupStartDateTime, device, sourceMediaFileInfo, targetDirectoryInfo, tempDirectoryInfo, config.CompareByBinary ?? true, filePattern, filePatternIfExist, fileProgress);
								}
								pendingFileInfo.IsSaved = isSaved;
								overallTask.Increment(1);
							}
						});
					/*
					AnsiConsole.Progress()
						.AutoClear(true)
						.AutoRefresh(true)
						.Start(ctx => {
							foreach(BackupPendingFileInfo pendingFileInfo in backupProgressTracker.PendingFiles.Values) {
								if(_cancellationToken.IsCancellationRequested) {
									Console.WriteLine("Backup afbrudt.");
									_cancellationToken.ThrowIfCancellationRequested();
								}

								if(pendingFileInfo.IsSaved) {
									continue;
								}

								//MediaFileInfo sourceMediaFileInfo = uniqueIdMediaFileInfos[pendingFileInfo.PersistentUniqueId];
								if(!uniqueIdMediaFileInfos.TryGetValue(pendingFileInfo.PersistentUniqueId, out var sourceMediaFileInfo)) {
									Console.WriteLine($"Fil ikke fundet: {pendingFileInfo.PersistentUniqueId}");
									continue;
								}

								var downloadFileTask = ctx.AddTask($"[green]Size: {sourceMediaFileInfo.Length}bytes, Downloading file : [/][white]{sourceMediaFileInfo.FullName}[/]", new ProgressTaskSettings { AutoStart = false, MaxValue = sourceMediaFileInfo.Length });
								Progress<FileProgressReport> fileProgress = new Progress<FileProgressReport>();
								fileProgress.ProgressChanged += (sender, report) => {
									downloadFileTask.Value(report.BytesRead);
								};

								if(!config.HasFilePattern()) {
									// BackupFromPath(device, fromPath, targetDirectoryPath, tempDirectoryInfo);
									throw new InvalidOperationException("Har ikke File Pattern, og mangler at implementere BackupFromPath(device, fromPath, targetDirectoryPath, tempDirectoryInfo);");
								} else {
									//if (String.IsNullOrEmpty(config.FilePattern))
									//{
									//    throw new ArgumentNullException("FilePattern er tom.");
									//}
									string filePattern = config.FilePattern!;

									//if (String.IsNullOrEmpty(config.FilePatternIfExist))
									//{
									//    throw new ArgumentNullException("FilePatternIfExist er tom.");
									//}
									string filePatternIfExist = config.FilePatternIfExist!;

									BackupFromPathWithFilePattern(backupStartDateTime, device, sourceMediaFileInfo, targetDirectoryInfo, tempDirectoryInfo, config.CompareByBinary ?? true, filePattern, filePatternIfExist, fileProgress);
									pendingFileInfo.IsSaved = true;
								}

							}
						});
						*/


					// TODO: Do this even when exception og cancel.
					// Delete the temp directory if it's empty
					BackupHelper.DeleteEmptyDirectories(tempDirectoryInfo.FullName);

					device.Disconnect();
				}
			} finally {
				backupProgressTracker.SaveDataStore();
				device.Disconnect();
			}
		}

		public void BackupDrive(DriveInfo drive, DriveSourceConfig config, DateTime backupStartDateTime) {
			Console.WriteLine($"Backing up drive: {drive.Name}");

			List<BackupRecordInfo> allDriveFiles = new List<BackupRecordInfo>();
			IDictionary<string, FileInfo> uniqueIdFileInfos;

			// Assuming the drive is already connected and accessible
			DirectoryInfo folderSourceDirectoryInfo;
			{
				DirectoryInfo? correctedFolderSource = ToCorrectFolderSource(drive, config.FolderSource);

				if(!ValidateFolderSource(correctedFolderSource)) {
					Console.WriteLine("FolderSource er ikke gyldig. Backup afbrudt.");
					Console.WriteLine($"Original FolderSource er {config.FolderSource}");
					return;
				}
				folderSourceDirectoryInfo = correctedFolderSource!;
			}


			Stopwatch stopwatch = Stopwatch.StartNew();
			IList<FileInfo> allFileInfoFiles = new List<FileInfo>();
			AnsiConsole.Progress()
				.AutoRefresh(true)
				.Columns(new ProgressColumn[] {
				new SpinnerColumn(new SequenceSpinner(SequenceSpinner.Sequence6)),
				new TaskDescriptionColumn(),
				new CounterColumn<int>("Count") { CounterStyle = new Style(decoration: Decoration.Bold) },
				new ElapsedTimeAdvancedColumn(),
				})
				.Start(ctx => {
					ProgressTask countingFilesTask = ctx.AddTask("Counting Files");
					countingFilesTask.IsIndeterminate = true;

					ProgressTask countingDirsTask = ctx.AddTask("Counting Dirs");
					countingDirsTask.IsIndeterminate = true;

					CancellationToken cancellationToken = _cancellationTokenGenerator.Token;

					if(cancellationToken.IsCancellationRequested) {
						cancellationToken.ThrowIfCancellationRequested();
					}

					Action<int> fileIncrementCallback = CreateIncrementCallback(count => { countingFilesTask.State.Update<int>("Count", _ => count); });
					Action<int> dirIncrementCallback = CreateIncrementCallback(count => { countingDirsTask.State.Update<int>("Count", _ => count); });
					FileAndDirectoryCounter progressReporter = new FileAndDirectoryCounter(fileIncrementCallback: fileIncrementCallback, dirIncrementCallback: dirIncrementCallback);
					allFileInfoFiles = ReadAllFiles(folderSourceDirectoryInfo, fileIncrementCallback, dirIncrementCallback, cancellationToken);
				});
			stopwatch.Stop();
			Console.WriteLine($"CreateBackupList took {stopwatch.ElapsedMilliseconds} ms");

			uniqueIdFileInfos = allFileInfoFiles.ToFrozenDictionary(fileInfo => fileInfo.FullName, fileInfo => fileInfo);

			foreach(var fileInfo in allFileInfoFiles) {
				BackupRecordInfo pendingFileInfo = new BackupRecordInfo(
					persistentUniqueId: fileInfo.FullName,
					path: fileInfo.FullName,
					size: Convert.ToUInt64(fileInfo.Length) // Assuming with conversion that Length is never negative
				) {
					Name = fileInfo.Name,
					DateCreated = fileInfo.CreationTime,
					DateModified = fileInfo.LastWriteTime,
					DateAuthored = fileInfo.LastWriteTime // Assuming DateAuthored is the same as LastWriteTime for files
				};
				pendingFileInfo.UpdateGeneratedId(); // TODO: Calculated another place.
				allDriveFiles.Add(pendingFileInfo);
			}

			// Sort for better backup progress.
			allDriveFiles.Sort((a, b) => string.Compare(a.Path, b.Path));
			BackupRecordDataStore backupProgressTracker = LoadProgress(config, allDriveFiles, exceptionIfChanged: true);

			try {
				// Validate FolderOutput, where the output of files should be.
				if(string.IsNullOrEmpty(config.FolderOutput)) {
					throw new ArgumentNullException(nameof(config.FolderOutput), "FolderOutput er null eller tom.");
				}
				if(config.FolderOutput.IndexOfAny(Path.GetInvalidPathChars()) >= 0) {
					throw new ArgumentException($"FolderOutput Path has invalid chars = '{config.FolderSource}'.", nameof(config.FolderSource));
				}
				if(File.Exists(config.FolderSource)) {
					throw new ArgumentException($"The Directory is a file = {config.FolderSource}", nameof(config.FolderSource));
				}
				DirectoryInfo targetDirectoryInfo = new DirectoryInfo(config.FolderOutput);
				if(!targetDirectoryInfo.Exists) {
					targetDirectoryInfo.Create();
					// Update DateTimes for directory as the same as source folder.
					BackupHelper.UpdateDateTimeForDirectoryInfo(folderSourceDirectoryInfo, targetDirectoryInfo);
				}
				DirectoryInfo tempDirectoryInfo = targetDirectoryInfo.CreateTempDirectory(BackupHelper.CreateTempDirectoryTimeStampPrefix(backupStartDateTime));

				Console.WriteLine();
				AnsiConsole.Progress()
					.AutoClear(true)
					.AutoRefresh(true)
					.HideCompleted(true)
					.Columns(new ProgressColumn[] {
				new SpinnerColumn(new SequenceSpinner(SequenceSpinner.Sequence7)),         // Spinner
                new TaskDescriptionColumn(), // Beskrivelse af opgaven
                new ProgressBarColumn() {Width=10},     // Fremdriftsbjælke
                new PercentageColumn(),      // Procentdel
                new RemainingTimeColumn(),   // Resterende tid
                new ValueOfMaxColumn(),
					})
					.Start(ctx => {
						var overallTask = ctx.AddTask("[green]Total Progress[/]", new ProgressTaskSettings { AutoStart = true, MaxValue = backupProgressTracker.Records.Count });
						int countFiles = 0;

						List<BackupRecordInfo> pendingFileInfos = backupProgressTracker.Records.ToList();
						pendingFileInfos.Sort((a, b) => string.Compare(a.Path, b.Path));
						foreach(BackupRecordInfo pendingFileInfo in pendingFileInfos) {
							countFiles++;
							overallTask.Description = $"[green]Total Progress[/]";
							if(_cancellationToken.IsCancellationRequested) {
								Console.WriteLine("Backup afbrudt.");
								_cancellationToken.ThrowIfCancellationRequested();
							}

							if(pendingFileInfo.IsSaved) {
								overallTask.Increment(1);
								continue;
							}

							if(!uniqueIdFileInfos.TryGetValue(pendingFileInfo.PersistentUniqueId, out var sourceFileInfo)) {
								Console.WriteLine($"Fil ikke fundet: {pendingFileInfo.PersistentUniqueId}");
								overallTask.Increment(1);
								continue;
							}
							var downloadFileTask = ctx.AddTask($"[green]Downloading file: [/][white]{BackupHelper.ShortenPath(sourceFileInfo.FullName, 27)}[/]", new ProgressTaskSettings { AutoStart = true, MaxValue = sourceFileInfo.Length });
							overallTask.Description = $"[green]Total Progress[/] - Downloading File: {sourceFileInfo.Name}";
							Progress<FileProgressReport> fileProgress = new Progress<FileProgressReport>();
							fileProgress.ProgressChanged += (sender, report) => {
								downloadFileTask.Value(report.BytesRead);
							};

							bool isSaved;
							if(!config.HasFilePattern()) {
								const bool addSideCarFile = false;
								isSaved = BackupFromPath(backupStartDateTime, drive, sourceFileInfo, targetDirectoryInfo, tempDirectoryInfo, config.CompareByBinary ?? true, addSideCarFile, fileProgress);
							} else {
								string filePattern = config.FilePattern!;
								string filePatternIfExist = config.FilePatternIfExist!;

								isSaved = BackupFromPathWithFilePattern(backupStartDateTime, drive, sourceFileInfo, targetDirectoryInfo, tempDirectoryInfo, config.CompareByBinary ?? true, filePattern, filePatternIfExist, fileProgress);
							}
							pendingFileInfo.IsSaved = isSaved;
							overallTask.Increment(1);
						}
					});

				// Delete the temp directory if it's empty
				BackupHelper.DeleteEmptyDirectories(tempDirectoryInfo.FullName);
			} finally {
				backupProgressTracker.SaveDataStore();
			}
		}
		bool BackupFromPath(DateTime backupStartDateTime, MediaDevice mediaDevice, MediaFileInfo sourceMediaFileInfo, DirectoryInfo targetDirectoryInfo, DirectoryInfo tempDirectoryInfo, bool compareByBinary, bool addSideCarFile, IProgress<FileProgressReport> fileProgress) {
			double kilobytes = sourceMediaFileInfo.Length / 1024.0;
			FileInfo targetTempFileInfo = DownloadToTempFile(tempDirectoryInfo, sourceMediaFileInfo, fileProgress);

			FileInfo? targetTempSideCarFileInfo = null;
			if(addSideCarFile) {
				targetTempSideCarFileInfo = CreateSideCarFileInfo(backupStartDateTime, mediaDevice, targetTempFileInfo, sourceMediaFileInfo);
			}

			IMetadataFileInfo metadataFileInfo = new MetadataExtractorFileInfo(targetTempFileInfo);
			DateTime? mediaCreatedDateTime = metadataFileInfo.GetCreatedMediaFileDateTime();
			DateTime fileCreatedDateTime = File.GetCreationTime(targetTempFileInfo.FullName);

			DateTime oldestDateTime = BackupHelper.ToOldestLegalDateTime(mediaCreatedDateTime, fileCreatedDateTime, DateTime.Now);
			string fileName = targetTempFileInfo.Name;

			string newTargetFilePath = Path.Combine(targetDirectoryInfo.FullName, sourceMediaFileInfo.Name);

			FileInfo newTargetFileInfo = new FileInfo(newTargetFilePath);

			if(newTargetFileInfo.Exists) {
				//FileComparer fileComparer = new ReadFileInChunksAndCompareAvx2(8 * 1024);
				//FileComparer fileComparer = new Md5Comparer(8 * 1024);
				//FileComparer fileComparer = new ReadFileInChunksAndCompareVector(8 * 1024);
				FileComparer fileComparer = _fileComparer;

				// Files are the same and we do not copy this file.
				if(fileComparer.Compare(targetTempFileInfo.FullName, newTargetFilePath)) {
					// Delete temp file and try next file.
					targetTempFileInfo.Delete();
					return true;
				}
				Console.WriteLine($"Your file {newTargetFilePath} exists and it is different, and I have NOT overwritten it.");
				return false;
			}
			if(!Directory.Exists(newTargetFileInfo.DirectoryName)) {
				Directory.CreateDirectory(newTargetFileInfo.DirectoryName!);
			}

			string? newTargetSideCarFilePath = null;
			if(addSideCarFile) {
				newTargetSideCarFilePath = newTargetFilePath + ".ini";
				if(Path.Exists(newTargetSideCarFilePath)) {
					throw new Exception($"File exists : {newTargetSideCarFilePath}");
				}
			}
			targetTempFileInfo.MoveTo(newTargetFilePath);
			if(addSideCarFile) {
				targetTempSideCarFileInfo?.MoveTo(newTargetSideCarFilePath!);
			}
			return true;
		}
		bool BackupFromPath(DateTime backupStartDateTime, DriveInfo driveInfo, FileInfo sourceFileInfo, DirectoryInfo targetDirectoryInfo, DirectoryInfo tempDirectoryInfo, bool compareByBinary, bool addSideCarFile, IProgress<FileProgressReport> fileProgress) {
			double kilobytes = sourceFileInfo.Length / 1024.0;
			FileInfo targetTempFileInfo = DownloadToTempFile(tempDirectoryInfo, sourceFileInfo, fileProgress);

			FileInfo? targetTempSideCarFileInfo = null;
			if(addSideCarFile) {
				targetTempSideCarFileInfo = CreateSideCarFileInfo(backupStartDateTime, driveInfo, targetTempFileInfo, sourceFileInfo);
			}

			IMetadataFileInfo metadataFileInfo = new MetadataExtractorFileInfo(targetTempFileInfo);
			DateTime? mediaCreatedDateTime = metadataFileInfo.GetCreatedMediaFileDateTime();
			DateTime fileCreatedDateTime = File.GetCreationTime(targetTempFileInfo.FullName);

			DateTime oldestDateTime = BackupHelper.ToOldestLegalDateTime(mediaCreatedDateTime, fileCreatedDateTime, DateTime.Now);
			string fileName = targetTempFileInfo.Name;

			string newTargetFilePath = Path.Combine(targetDirectoryInfo.FullName, sourceFileInfo.Name);

			FileInfo newTargetFileInfo = new FileInfo(newTargetFilePath);

			if(newTargetFileInfo.Exists) {
				//FileComparer fileComparer = new ReadFileInChunksAndCompareAvx2(8 * 1024);
				//FileComparer fileComparer = new Md5Comparer(8 * 1024);
				//FileComparer fileComparer = new ReadFileInChunksAndCompareVector(8 * 1024);
				FileComparer fileComparer = _fileComparer;

				// Files are the same and we do not copy this file.
				if(fileComparer.Compare(targetTempFileInfo.FullName, newTargetFilePath)) {
					// Delete temp file and try next file.
					targetTempFileInfo.Delete();
					return true;
				}
				Console.WriteLine($"Your file {newTargetFilePath} exists and it is different, and I have NOT overwritten it.");
				return false;
			}
			if(!Directory.Exists(newTargetFileInfo.DirectoryName)) {
				Directory.CreateDirectory(newTargetFileInfo.DirectoryName!);
			}

			string? newTargetSideCarFilePath = null;
			if(addSideCarFile) {
				newTargetSideCarFilePath = newTargetFilePath + ".ini";
				if(Path.Exists(newTargetSideCarFilePath)) {
					throw new Exception($"File exists : {newTargetSideCarFilePath}");
				}
			}
			targetTempFileInfo.MoveTo(newTargetFilePath);
			if(addSideCarFile) {
				targetTempSideCarFileInfo?.MoveTo(newTargetSideCarFilePath!);
			}
			return true;
		}


		bool BackupFromPathWithFilePattern(DateTime backupStartDateTime, MediaDevice mediaDevice, MediaFileInfo sourceMediaFileInfo, DirectoryInfo targetDirectoryInfo, DirectoryInfo tempDirectoryInfo, bool compareByBinary, string filePattern, string filePatternIfExist, IProgress<FileProgressReport> fileProgress) {
			double kilobytes = sourceMediaFileInfo.Length / 1024.0;
			FileInfo targetTempFileInfo = DownloadToTempFile(tempDirectoryInfo, sourceMediaFileInfo, fileProgress);

			FileInfo targetTempSideCarFileInfo = CreateSideCarFileInfo(backupStartDateTime, mediaDevice, targetTempFileInfo, sourceMediaFileInfo);

			//MetadataFileInfo metadataFileInfo = new MetadataFileInfo(targetTempFileInfo);
			IMetadataFileInfo metadataFileInfo = new MetadataExtractorFileInfo(targetTempFileInfo);
			DateTime? mediaCreatedDateTime = metadataFileInfo.GetCreatedMediaFileDateTime();
			DateTime fileCreatedDateTime = File.GetCreationTime(targetTempFileInfo.FullName);

			DateTime oldestDateTime = BackupHelper.ToOldestLegalDateTime(mediaCreatedDateTime, fileCreatedDateTime, DateTime.Now);
			string fileName = targetTempFileInfo.Name;
			int counter = 0;
			Template template = CreateTemplateFrom(counter, fileName, oldestDateTime);

			string filePatternTargetFilePath = Path.Combine(targetDirectoryInfo.FullName, filePattern);
			string filePatternIfExistTargetFilePath = Path.Combine(targetDirectoryInfo.FullName, filePatternIfExist);

			string newTargetFilePath = template.Replace(filePatternTargetFilePath);
			FileInfo newTargetFileInfo = new FileInfo(newTargetFilePath);

			if(newTargetFileInfo.Exists) {
				//FileComparer fileComparer = new ReadFileInChunksAndCompareAvx2(8 * 1024);
				//FileComparer fileComparer = new Md5Comparer(8 * 1024);
				//FileComparer fileComparer = new ReadFileInChunksAndCompareVector(8 * 1024);
				FileComparer fileComparer = _fileComparer;

				// Files are the same and we do not copy this file.
				if(fileComparer.Compare(targetTempFileInfo.FullName, newTargetFilePath)) {
					// Delete temp file and try next file.
					targetTempFileInfo.Delete();
					return true;
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
						return false;
					}

				}
			}
			if(!Directory.Exists(newTargetFileInfo.DirectoryName)) {
				Directory.CreateDirectory(newTargetFileInfo.DirectoryName!);
			}
			string newTargetSideCarFilePath = newTargetFilePath + ".ini";
			if(Path.Exists(newTargetSideCarFilePath)) {
				throw new Exception($"File exists : {newTargetSideCarFilePath}");
			}

			//Console.WriteLine("FileUsed: " + newTargetFileInfo.FullName);
			targetTempFileInfo.MoveTo(newTargetFilePath);
			targetTempSideCarFileInfo.MoveTo(newTargetSideCarFilePath);

			//Console.WriteLine($"Fil {fileName} kopieret til {newTargetFilePath}, Size: {kilobytes:F2} KB");

			return true;
		}
		bool BackupFromPathWithFilePattern(DateTime backupStartDateTime, DriveInfo driveInfo, FileInfo sourceFileInfo, DirectoryInfo targetDirectoryInfo, DirectoryInfo tempDirectoryInfo, bool compareByBinary, string filePattern, string filePatternIfExist, IProgress<FileProgressReport> fileProgress) {
			double kilobytes = sourceFileInfo.Length / 1024.0;
			FileInfo targetTempFileInfo = DownloadToTempFile(tempDirectoryInfo, sourceFileInfo, fileProgress);

			FileInfo targetTempSideCarFileInfo = CreateSideCarFileInfo(backupStartDateTime, driveInfo, targetTempFileInfo, sourceFileInfo);

			//MetadataFileInfo metadataFileInfo = new MetadataFileInfo(targetTempFileInfo);
			IMetadataFileInfo metadataFileInfo = new MetadataExtractorFileInfo(targetTempFileInfo);
			DateTime? mediaCreatedDateTime = metadataFileInfo.GetCreatedMediaFileDateTime();
			DateTime fileCreatedDateTime = File.GetCreationTime(targetTempFileInfo.FullName);

			DateTime oldestDateTime = BackupHelper.ToOldestLegalDateTime(mediaCreatedDateTime, fileCreatedDateTime, DateTime.Now);
			string fileName = targetTempFileInfo.Name;
			int counter = 0;
			Template template = CreateTemplateFrom(counter, fileName, oldestDateTime);

			string filePatternTargetFilePath = Path.Combine(targetDirectoryInfo.FullName, filePattern);
			string filePatternIfExistTargetFilePath = Path.Combine(targetDirectoryInfo.FullName, filePatternIfExist);

			string newTargetFilePath = template.Replace(filePatternTargetFilePath);
			FileInfo newTargetFileInfo = new FileInfo(newTargetFilePath);

			if(newTargetFileInfo.Exists) {
				//FileComparer fileComparer = new ReadFileInChunksAndCompareAvx2(8 * 1024);
				//FileComparer fileComparer = new Md5Comparer(8 * 1024);
				//FileComparer fileComparer = new ReadFileInChunksAndCompareVector(8 * 1024);
				FileComparer fileComparer = _fileComparer;

				// Files are the same and we do not copy this file.
				if(fileComparer.Compare(targetTempFileInfo.FullName, newTargetFilePath)) {
					// Delete temp file and try next file.
					targetTempFileInfo.Delete();
					return true;
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
						return false;
					}

				}
			}
			if(!Directory.Exists(newTargetFileInfo.DirectoryName)) {
				Directory.CreateDirectory(newTargetFileInfo.DirectoryName!);
			}
			string newTargetSideCarFilePath = newTargetFilePath + ".ini";
			if(Path.Exists(newTargetSideCarFilePath)) {
				throw new Exception($"File exists : {newTargetSideCarFilePath}");
			}

			//Console.WriteLine("FileUsed: " + newTargetFileInfo.FullName);
			targetTempFileInfo.MoveTo(newTargetFilePath);
			targetTempSideCarFileInfo.MoveTo(newTargetSideCarFilePath);

			//Console.WriteLine($"Fil {fileName} kopieret til {newTargetFilePath}, Size: {kilobytes:F2} KB");

			return true;
		}
		FileInfo CreateSideCarFileInfo(DateTime backupStartDateTime, MediaDevice mediaDevice, FileInfo targetTempFileInfo, MediaFileInfo mediaFileInfo) {
			string fullName = targetTempFileInfo.FullName;
			string name = targetTempFileInfo.Name;
			//string nameWithoutExtension = Path.GetFileNameWithoutExtension(fullName);
			string extension = ".ini";
			if(targetTempFileInfo.DirectoryName == null) throw new Exception("Something is wrong, I do not know why this directoryName is null: " + targetTempFileInfo.FullName);
			string sideCarFullPath = Path.Combine(targetTempFileInfo.DirectoryName, name + extension);

			using(StreamWriter writer = new StreamWriter(sideCarFullPath)) {
				IMetadataFileInfo metadataFileInfo = new MetadataExtractorFileInfo(targetTempFileInfo);

				string mediaFilePersistentUniqueId = mediaFileInfo.PersistentUniqueId;
				string mediaFileFullName = mediaFileInfo.FullName;

				string deviceId = mediaDevice.DeviceId;
				string deviceDescription = mediaDevice.Description;
				string deviceFriendlyName = mediaDevice.FriendlyName;
				string deviceManufacturer = mediaDevice.Manufacturer;
				string deviceModel = mediaDevice.Model;
				string deviceSerialNumber = mediaDevice.SerialNumber;

				List<HashCalculator.HashType> hashTypes = [
						SHA3_512_KECCAK,
						SHA3_512_FIPS202,
						SHA2_256,
						MD5_128,
						BLAKE3_256,
						BLAKE3_512,

					];
				IDictionary<HashCalculator.HashType, string> hashes = BackupHelper.ComputeHashes(fullName, hashTypes);
				string sha3_512_keccak_str = hashes[SHA3_512_KECCAK];
				string sha3_512_fips202_str = hashes[SHA3_512_FIPS202];
				string sha2_256_str = hashes[SHA2_256];
				string md5_128 = hashes[MD5_128];
				string blake3_256_str = hashes[BLAKE3_256];
				string blake3_512_str = hashes[BLAKE3_512];

				/*
OriginalFileName=IMG_20230222_140525_807.jpg
CurrentFileName=IMG_20230222_140525_807_backup.jpg
CreatedDateTime=2024-08-03T12:16:10.5825807Z
LastAccessedDateTime=2024-08-03T12:16:21.5781213Z
LastModifiedDateTime=2024-08-03T12:16:10.5825807Z
MediaTakenDateTime=2023-02-22T13:05:25.0000000Z
				 * */
				writer.WriteLine("[Settings]");
				writer.WriteLine("OriginalFileName=" + targetTempFileInfo.Name);
				/*
				writer.WriteLine("CreateDateTime=" + targetTempFileInfo.CreationTime.ToUniversalTime().ToString("o"));
				writer.WriteLine("LastAccessDateTime=" + targetTempFileInfo.LastAccessTime.ToUniversalTime().ToString("o"));
				writer.WriteLine("LastWriteDateTime=" + targetTempFileInfo.LastWriteTime.ToUniversalTime().ToString("o"));
				writer.WriteLine("MediaTakenDateTime=" + metadataFileInfo.GetCreatedMediaFileDateTime()?.ToUniversalTime().ToString("o"));
				*/
				writer.WriteLine("CreateDateTime=" + targetTempFileInfo.CreationTime.ToString("o"));
				writer.WriteLine("LastAccessDateTime=" + targetTempFileInfo.LastAccessTime.ToString("o"));
				writer.WriteLine("LastWriteDateTime=" + targetTempFileInfo.LastWriteTime.ToString("o"));
				writer.WriteLine("MediaTakenDateTime=" + metadataFileInfo.GetCreatedMediaFileDateTime()?.ToString("o"));
				writer.WriteLine();

				writer.WriteLine("[BackupInfo]");
				writer.WriteLine("BackupDateTime=" + backupStartDateTime.ToString("o"));
				writer.WriteLine();

				writer.WriteLine("[FileHash]");
				writer.WriteLine("SHA3_512_KECCAK=" + sha3_512_keccak_str);
				writer.WriteLine("SHA3_512_FIPS202=" + sha3_512_fips202_str);
				writer.WriteLine("SHA2_256=" + sha2_256_str);
				writer.WriteLine("MD5=" + md5_128);
				writer.WriteLine("BLAKE3_256=" + blake3_256_str);
				writer.WriteLine("BLAKE3_512=" + blake3_512_str);
				writer.WriteLine();

				writer.WriteLine("[DeviceFileDetails]");
				writer.WriteLine("PersistentUniqueId=" + mediaFilePersistentUniqueId);
				writer.WriteLine("FullName=" + mediaFileFullName);
				writer.WriteLine();

				writer.WriteLine("[DeviceDetails]");
				writer.WriteLine("DeviceId=" + deviceId);
				writer.WriteLine("Description=" + deviceDescription);
				writer.WriteLine("FriendlyName=" + deviceFriendlyName);
				writer.WriteLine("Manufacturer=" + deviceManufacturer);
				writer.WriteLine("Model=" + deviceModel);
				writer.WriteLine("SerialNumber=" + deviceSerialNumber);

			}
			return new FileInfo(sideCarFullPath);
		}
		FileInfo CreateSideCarFileInfo(DateTime backupStartDateTime, DriveInfo driveInfo, FileInfo targetTempFileInfo, FileInfo fileInfo) {
			string fullName = targetTempFileInfo.FullName;
			string name = targetTempFileInfo.Name;
			string extension = ".ini";
			if(targetTempFileInfo.DirectoryName == null) throw new Exception("Something is wrong, I do not know why this directoryName is null: " + targetTempFileInfo.FullName);
			string sideCarFullPath = Path.Combine(targetTempFileInfo.DirectoryName, name + extension);

			using(StreamWriter writer = new StreamWriter(sideCarFullPath)) {
				IMetadataFileInfo metadataFileInfo = new MetadataExtractorFileInfo(targetTempFileInfo);

				string filePersistentUniqueId = fileInfo.FullName;
				string fileFullName = fileInfo.FullName;

				string driveId = driveInfo.Name;
				string driveDescription = driveInfo.DriveType.ToString();
				string driveVolumeLabel = driveInfo.VolumeLabel;

				List<HashCalculator.HashType> hashTypes = [
					SHA3_512_KECCAK,
					SHA3_512_FIPS202,
					SHA2_256,
					MD5_128,
					BLAKE3_256,
					BLAKE3_512,
				];

				IDictionary<HashCalculator.HashType, string> hashes = BackupHelper.ComputeHashes(fileInfo.FullName, hashTypes);
				string sha3_512_keccak_str = hashes[SHA3_512_KECCAK];
				string sha3_512_fips202_str = hashes[SHA3_512_FIPS202];
				string sha2_256_str = hashes[SHA2_256];
				string md5_128 = hashes[MD5_128];
				string blake3_256_str = hashes[BLAKE3_256];
				string blake3_512_str = hashes[BLAKE3_512];

				writer.WriteLine("[Settings]");
				writer.WriteLine("OriginalFileName=" + targetTempFileInfo.Name);
				writer.WriteLine("CreateDateTime=" + targetTempFileInfo.CreationTime.ToString("o"));
				writer.WriteLine("LastAccessDateTime=" + targetTempFileInfo.LastAccessTime.ToString("o"));
				writer.WriteLine("LastWriteDateTime=" + targetTempFileInfo.LastWriteTime.ToString("o"));
				writer.WriteLine("MediaTakenDateTime=" + metadataFileInfo.GetCreatedMediaFileDateTime()?.ToString("o"));
				writer.WriteLine();

				writer.WriteLine("[BackupInfo]");
				writer.WriteLine("BackupDateTime=" + backupStartDateTime.ToString("o"));
				writer.WriteLine();

				writer.WriteLine("[FileHash]");
				writer.WriteLine("SHA3_512_KECCAK=" + sha3_512_keccak_str);
				writer.WriteLine("SHA3_512_FIPS202=" + sha3_512_fips202_str);
				writer.WriteLine("SHA2_256=" + sha2_256_str);
				writer.WriteLine("MD5=" + md5_128);
				writer.WriteLine("BLAKE3_256=" + blake3_256_str);
				writer.WriteLine("BLAKE3_512=" + blake3_512_str);
				writer.WriteLine();

				writer.WriteLine("[DriveFileDetails]");
				writer.WriteLine("PersistentUniqueId=" + filePersistentUniqueId);
				writer.WriteLine("FullName=" + fileFullName);
				writer.WriteLine();

				writer.WriteLine("[DriveDetails]");
				writer.WriteLine("DriveId=" + driveId);
				writer.WriteLine("Description=" + driveDescription);
				writer.WriteLine("VolumeLabel=" + driveVolumeLabel);
			}
			return new FileInfo(sideCarFullPath);
		}

		Template CreateTemplateFrom(int counter, string fileName, DateTime sourceDateTime) {
			Template template = new Template();

			string year4 = sourceDateTime.ToString("yyyy");
			string month2 = sourceDateTime.ToString("MM");
			string day2 = sourceDateTime.ToString("dd");
			string hour24 = sourceDateTime.ToString("HH");
			string minutes2 = sourceDateTime.ToString("mm");
			string seconds2 = sourceDateTime.ToString("ss");
			string milliseconds3 = sourceDateTime.ToString("fff");
			string originalName = Path.GetFileNameWithoutExtension(fileName);
			string extension = Path.GetExtension(fileName).TrimStart('.');

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

			return template;
		}

		FileInfo DownloadToTempFile(DirectoryInfo tempDirectoryInfo, MediaFileInfo sourceMediaFileInfo, IProgress<FileProgressReport> fileProgress) {
			string fileName = sourceMediaFileInfo.Name;
			FileInfo targetTempFileInfo = tempDirectoryInfo.CombineToFileInfo(fileName);
			/*
						AnsiConsole.Progress()
							.AutoClear(true)
							.AutoRefresh(true)
							.Start(ctx => {
							var downloadFileTask = ctx.AddTask($"[green]Downloading file : [/][white]{sourceMediaFileInfo.FullName}[/]", new ProgressTaskSettings { AutoStart=false, MaxValue = sourceMediaFileInfo.Length});
							var fileProgress = new Progress<FileProgressReport>();
							fileProgress.ProgressChanged += (sender, report) => {
								downloadFileTask.Value(report.BytesRead);
							};
							sourceMediaFileInfo.CopyTo(targetTempFileInfo.FullName, fileProgress);
						});
						*/
			sourceMediaFileInfo.CopyTo(targetTempFileInfo.FullName, fileProgress);

			//sourceMediaFileInfo.CopyTo(targetTempFileInfo.FullName);
			BackupHelper.UpdateDateTimeForFileInfo(sourceMediaFileInfo, targetTempFileInfo);

			return targetTempFileInfo;
		}
		FileInfo DownloadToTempFile(DirectoryInfo tempDirectoryInfo, FileInfo sourceFileInfo, IProgress<FileProgressReport> fileProgress) {
			string fileName = sourceFileInfo.Name;
			FileInfo targetTempFileInfo = tempDirectoryInfo.CombineToFileInfo(fileName);

			using(FileStream sourceStream = sourceFileInfo.OpenRead())
			using(FileStream targetStream = targetTempFileInfo.OpenWrite()) {
				DateTime startDateTime = DateTime.Now;

				byte[] buffer = new byte[8192];
				int bytesRead;
				ulong totalBytesRead = 0;

				while((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0) {
					targetStream.Write(buffer, 0, bytesRead);
					totalBytesRead += (ulong)bytesRead;

					// Report progress
					DateTime reportDateTime = DateTime.Now;
					fileProgress.Report(new FileProgressReport(totalBytesRead, (ulong)sourceFileInfo.Length, startDateTime, reportDateTime, reportDateTime.Subtract(startDateTime)));
				}
			}
			BackupHelper.UpdateDateTimeForFileInfo(sourceFileInfo, targetTempFileInfo);

			return targetTempFileInfo;
		}
		private MediaDirectoryInfo ValidateAndCorrectFolderSourcePathToMediaDirectoryInfo(MediaDevice mediaDevice, string? folderSource) {
			ValidateFolderSource(folderSource);
			string correctedFolderSource = CorrectFolderSource(mediaDevice, folderSource!);
			return GetMediaDirectoryInfoIfExists(mediaDevice, correctedFolderSource, folderSource!);
		}
		private void ValidateFolderSource(string? folderSource) {
			if(string.IsNullOrEmpty(folderSource)) {
				throw new ArgumentNullException($"FolderSource is empty.");
			}
		}
		private string CorrectFolderSource(MediaDevice mediaDevice, string folderSource) {
			string correctedFolderSource = folderSource;
			try {
				string rootSource = GetRootSource(folderSource);
				// We are using this as a way to remove FriendlyName, if set in the FolderSource
				// But sometimes the FriendlyName is null og empty.
				string rootSourceFoundName = GetRootSourceFoundName(mediaDevice);
				if(IsRootSourceMatchingDeviceName(rootSource, rootSourceFoundName) && !mediaDevice.DirectoryExists(folderSource)) {
					// Removes rootSource if same as Device Name.
					// And to be sure that we do not remove it if it is a correctfolder then test if it does not exists
					correctedFolderSource = RemoveRootSource(folderSource, rootSource);
				}
				} catch(COMException e) {
				HandleCOMException(e, mediaDevice);
				Console.WriteException(e);
			}
			return correctedFolderSource;
		}
		private string GetRootSource(string folderSource) {
			return folderSource.Split(new char[] { '/', '\\' })[0];
		}
		private string GetRootSourceFoundName(MediaDevice mediaDevice) {
			// We are using this as a way to remove FriendlyName, if set in the FolderSource
			// But sometimes the FriendlyName is null og empty.
			return !string.IsNullOrWhiteSpace(mediaDevice.FriendlyName) ? mediaDevice.FriendlyName :
				   !string.IsNullOrWhiteSpace(mediaDevice.Description) ? mediaDevice.Description : mediaDevice.Model;
		}
		private bool IsRootSourceMatchingDeviceName(string rootSource, string rootSourceFoundName) {
			return rootSource.Equals(rootSourceFoundName, StringComparison.Ordinal);
		}
		private string RemoveRootSource(string folderSource, string rootSource) {
			return folderSource.Substring(rootSource.Length + 1);
		}
		private void HandleCOMException(COMException e, MediaDevice mediaDevice) {
			if(e.Message.Contains("(0x800710D2)")) {
				logger.ZLogError($"The library, drive, or media pool is empty. (0x800710D2)");
				throw new Exception($"The Device '{mediaDevice.FriendlyName}' exists but is empty. Please open and activate the physical device.");
			} else {
				logger.ZLogError($"COMException occurred: {e.Message}");
				throw new Exception($"COMException occurred: {e.Message}", e);
			}
		}
		private MediaDirectoryInfo GetMediaDirectoryInfoIfExists(MediaDevice mediaDevice, string correctedFolderSource, string folderSource) {
			if(!mediaDevice.DirectoryExists(correctedFolderSource)) {
				throw new Exception($"FolderSource '{folderSource}' does not exist on the device.");
			}
			return mediaDevice.GetDirectoryInfo(correctedFolderSource);
		}
		private DirectoryInfo? ToCorrectFolderSource(DriveInfo drive, string? folderSource) {
			ValidateFolderSource(folderSource);

			string correctedFolderSource = CorrectFolderSource(drive, folderSource!);

			return GetDirectoryInfoIfExists(correctedFolderSource);
		}
		private string CorrectFolderSource(DriveInfo drive, string folderSource) {
			string correctedFolderSource = folderSource;

			string rootSource = folderSource.Split(new char[] { '/', '\\' })[0];
			string rootSourceFoundName = drive.Name.TrimEnd('\\');
			if(rootSource.Equals(rootSourceFoundName, StringComparison.Ordinal)
				&& !Directory.Exists(folderSource)) {
				// Removes rootSource if same as Drive Name.
				correctedFolderSource = folderSource.Substring(folderSource.Split(new char[] { '/', '\\' })[0].Length + 1);
			}
			return correctedFolderSource;
		}
		private DirectoryInfo? GetDirectoryInfoIfExists(string correctedFolderSource) {
			if(!Directory.Exists(correctedFolderSource)) {
				//throw new Exception($"FolderSource '{folderSource}' does not exist on the drive.");
				return null;
			}
			return new DirectoryInfo(correctedFolderSource);
		}
		private bool ValidateFolderSource(DirectoryInfo? folderSource) {
			if(null == folderSource) {
				return false;
			}
			if(!Directory.Exists(folderSource.FullName)) {
				return false;
			}
			return true;
		}
		private BackupRecordDataStore UpdateProgressList(BackupRecordDataStore backupProgressTracker, IList<MediaFileInfo> allFiles, bool exceptionIfChanges) {
			// TODO - Should be used when backupProgressTracker should be changed because newer files needs to be added
			throw new NotImplementedException();
		}
		private BackupRecordDataStore LoadProgress(DeviceSourceConfig config, IList<BackupRecordInfo> allFiles, bool exceptionIfChanged) {
			BackupRecordDataStore progressTracker;
			if(BackupRecordDataStore.HasDataStore(config)) {
				progressTracker = BackupRecordDataStore.LoadDataStore(config);
				// TODO - Should be used when backupProgressTracker should be changed because newer files needs to be added
				bool equalsPending = progressTracker.Records.SequenceEqual(allFiles);
				if(!equalsPending) {
					var added = allFiles.Except(progressTracker.Records).ToList();
					var removed = progressTracker.Records.Except(allFiles).ToList();

					// Test if added or removed is equals with generatedId
					var pendingFileIds = progressTracker.Records.Select(br => br.GeneratedId).ToHashSet();
					var currentFileIds = allFiles.Select(fd => fd.GeneratedId).ToHashSet();

					var addedUsingGeneratedId = allFiles
						.Where(br => !pendingFileIds.Contains(br.GeneratedId))
						.ToList();

					var removedUsingGeneratedId = allFiles
						.Where(br => !currentFileIds.Contains(br.GeneratedId))
						.ToList();

					if(exceptionIfChanged) {
						Console.MarkupLine($"You have [bold]more[/] files on device than Resume JSON: [green bold]{added.Count}[/]");
						Console.MarkupLine($"You have [bold]less[/] files on device than Resume JSON: [bold]{added.Count}[/]");
						logger.ZLogInformation($"You have an updated device, please delete file: {progressTracker.GetDataStoreFileInfo().FullName}");
						throw new Exception("You have an updated device, please delete file: " + progressTracker.GetDataStoreFileInfo().FullName);
					} else {
						// Write over old progresstracker
						progressTracker = new BackupRecordDataStore(config, allFiles);
						FileInfo progressFileInfo = progressTracker.SaveDataStore();
					}
				}
			} else {
				progressTracker = new BackupRecordDataStore(config, allFiles);
				FileInfo progressFileInfo = progressTracker.SaveDataStore();
			}

			return progressTracker;
		}
		private BackupRecordDataStore LoadProgress(DriveSourceConfig config, IList<BackupRecordInfo> allFiles, bool exceptionIfChanged) {
			BackupRecordDataStore progressTracker;
			if(BackupRecordDataStore.HasDataStore(config)) {
				progressTracker = BackupRecordDataStore.LoadDataStore(config);
				// TODO - Should be used when backupProgressTracker should be changed because newer files needs to be added
				bool equalsPending = progressTracker.Records.SequenceEqual(allFiles);
				if(!equalsPending) {
					var added = allFiles.Except(progressTracker.Records).ToList();
					var removed = progressTracker.Records.Except(allFiles).ToList();

					// Test if added or removed is equals with generatedId
					var pendingFileIds = progressTracker.Records.Select(br => br.GeneratedId).ToHashSet();
					var currentFileIds = allFiles.Select(fd => fd.GeneratedId).ToHashSet();

					var addedUsingGeneratedId = allFiles
						.Where(br => !pendingFileIds.Contains(br.GeneratedId))
						.ToList();

					var removedUsingGeneratedId = allFiles
						.Where(br => !currentFileIds.Contains(br.GeneratedId))
						.ToList();

					if(exceptionIfChanged) {
						Console.MarkupLine($"You have [bold]more[/] files on device than Resume JSON: [green bold]{added.Count}[/]");
						Console.MarkupLine($"You have [bold]less[/] files on device than Resume JSON: [bold]{added.Count}[/]");
						logger.ZLogInformation($"You have an updated device, please delete file: {progressTracker.GetDataStoreFileInfo().FullName}");
						throw new Exception("You have an updated device, please delete file: " + progressTracker.GetDataStoreFileInfo().FullName);
					} else {
						// Write over old progresstracker
						progressTracker = new BackupRecordDataStore(config, allFiles);
						FileInfo progressFileInfo = progressTracker.SaveDataStore();
					}
				}
			} else {
				progressTracker = new BackupRecordDataStore(config, allFiles);
				FileInfo progressFileInfo = progressTracker.SaveDataStore();
			}

			return progressTracker;
		}
		private IList<MediaFileInfo> ReadAllFiles(MediaDirectoryInfo fromDirectoryInfo, Action<int> fileIncrementCallback, Action<int> dirIncrementCallback, CancellationToken cancellationToken) {
			if(cancellationToken.IsCancellationRequested) {
				throw new OperationCanceledException($"ReadAllFiles current directory: {fromDirectoryInfo.FullName}", cancellationToken);
			}

			IList<MediaFileInfo> filesFound = new List<MediaFileInfo>();

			foreach(var file in fromDirectoryInfo.EnumerateFiles()) {
				filesFound.Add(file);
				fileIncrementCallback(1);
				if(cancellationToken.IsCancellationRequested) {
					throw new OperationCanceledException($"ReadAllFiles current directory: {fromDirectoryInfo.FullName}", cancellationToken);
				}
			}


			foreach(MediaDirectoryInfo directory in fromDirectoryInfo.EnumerateDirectories()) {
				if(cancellationToken.IsCancellationRequested) {
					throw new OperationCanceledException($"ReadAllFiles current directory: {directory.FullName}", cancellationToken);
				}
				dirIncrementCallback(1);
				foreach(var file in ReadAllFiles(directory, fileIncrementCallback, dirIncrementCallback, cancellationToken)) {
					filesFound.Add(file);
				}
			}
			return filesFound;
		}
		private IList<FileInfo> ReadAllFiles(DirectoryInfo fromDirectoryInfo, Action<int> fileIncrementCallback, Action<int> dirIncrementCallback, CancellationToken cancellationToken) {
			if(cancellationToken.IsCancellationRequested) {
				throw new OperationCanceledException($"ReadAllFiles current directory: {fromDirectoryInfo.FullName}", cancellationToken);
			}

			IList<FileInfo> filesFound = new List<FileInfo>();

			foreach(var file in fromDirectoryInfo.EnumerateFiles()) {
				filesFound.Add(file);
				fileIncrementCallback(1);
				if(cancellationToken.IsCancellationRequested) {
					throw new OperationCanceledException($"ReadAllFiles current directory: {fromDirectoryInfo.FullName}", cancellationToken);
				}
			}

			foreach(var directory in fromDirectoryInfo.EnumerateDirectories()) {
				if(cancellationToken.IsCancellationRequested) {
					throw new OperationCanceledException($"ReadAllFiles current directory: {directory.FullName}", cancellationToken);
				}
				dirIncrementCallback(1);
				foreach(var file in ReadAllFiles(directory, fileIncrementCallback, dirIncrementCallback, cancellationToken)) {
					filesFound.Add(file);
				}
			}
			return filesFound;
		}
		private async Task<IList<MediaFileInfo>> ReadAllFilesAsync(MediaDirectoryInfo fromDirectoryInfo) {
			if(_cancellationToken.IsCancellationRequested) {
				throw new OperationCanceledException($"ReadAllFiles current directory: {fromDirectoryInfo.FullName}", _cancellationToken);
			}

			var filesTask = Task.Run(() => fromDirectoryInfo.EnumerateFiles().ToList(), _cancellationToken);
			var directoriesTask = Task.Run(() => fromDirectoryInfo.EnumerateDirectories().ToList(), _cancellationToken);

			await Task.WhenAll(filesTask, directoriesTask);

			var directoryResults = await Task.WhenAll(directoriesTask.Result.Select(directory => ReadAllFilesAsync(directory)));

			IList<MediaFileInfo> allFiles = filesTask.Result.Concat(directoryResults.SelectMany(r => r)).ToList();

			return allFiles;
		}
	}
}
