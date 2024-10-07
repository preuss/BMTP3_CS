using BMTP3_CS.CompareFiles;
using BMTP3_CS.Metadata;
using BMTP3_CS.StringVariableSubstitution;
using MediaDevices.Progress;
using MediaDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using SHA3.Net;

namespace BMTP3_CS.Handlers.Backup {
	[SupportedOSPlatform("windows7.0")]
	public class BackupHelper {
		private bool IsInvalidDateTime(DateTime? sourceDateTime) {
			return !IsValidDateTime(sourceDateTime);
		}
		private bool IsValidDateTime(DateTime? sourceDateTime) {
			return DateTime.MinValue.CompareTo(sourceDateTime) < 0;
		}
		public DateTime FindEarliestValidDateTime(params DateTime?[] dateTimes) {
			List<DateTime> validDateTimes = dateTimes
				.Where(dateTime => IsValidDateTime(dateTime))
				.Select(dateTime => dateTime!.Value)
				.OrderBy(dateTime => dateTime)
				.ToList();

			if(validDateTimes.Count == 0) {
				throw new ArgumentException("No valid default dates provided.");
			}

			return validDateTimes.First();
		}
		private DateTime FindValidDateTimeOrEarliestValidDateTime(DateTime? sourceDateTime, params DateTime?[] defaultDateTimes) {
			if(IsInvalidDateTime(sourceDateTime)) {
				return FindEarliestValidDateTime(defaultDateTimes);
			}
			return sourceDateTime!.Value;
		}
		public bool DeleteEmptyDirectoriesRecursive(string path) {
			bool isEmpty = true;

			foreach(string dir in Directory.GetDirectories(path)) {
				if(!DeleteEmptyDirectoriesRecursive(dir)) {
					isEmpty = false;
				}
			}

			if(isEmpty && Directory.GetFiles(path).Length == 0) {
				Directory.Delete(path);
				return true;
			}
			return false;
		}
		public string CreateTempDirectory(DateTime fromDateTime) {
			string timestamp = fromDateTime.ToString("yyyyMMdd_HHmmss");
			string tempDirectoryName = $"Temp_{timestamp}_";
			return tempDirectoryName;
		}
		public void UpdateDirectoryTimestamp(MediaDirectoryInfo sourceInfo, DirectoryInfo targetInfo) {
			DateTime?[] defaultDateTimes = [sourceInfo.DateAuthored, sourceInfo.CreationTime, sourceInfo.LastWriteTime, DateTime.Now];
			targetInfo.CreationTime = FindEarliestValidDateTime(defaultDateTimes);
			targetInfo.LastAccessTime = FindEarliestValidDateTime(defaultDateTimes);
			targetInfo.LastWriteTime = FindEarliestValidDateTime(defaultDateTimes);

			// Because DirectoryInfo is only a cached representation
			Directory.SetCreationTime(targetInfo.FullName, targetInfo.CreationTime);
			Directory.SetLastAccessTime(targetInfo.FullName, targetInfo.LastAccessTime);
			Directory.SetLastWriteTime(targetInfo.FullName, targetInfo.LastWriteTime);
		}
		public void UpdateFileTimestamp(MediaFileInfo sourceInfo, FileInfo targetInfo) {
			DateTime?[] defaultDateTimes = [sourceInfo.DateAuthored, sourceInfo.CreationTime, sourceInfo.LastWriteTime, DateTime.Now];
			targetInfo.CreationTime = FindEarliestValidDateTime(defaultDateTimes);
			targetInfo.LastAccessTime = FindEarliestValidDateTime(defaultDateTimes);
			targetInfo.LastWriteTime = FindEarliestValidDateTime(defaultDateTimes);

			// Because FileInfo is only a cached representation
			File.SetCreationTime(targetInfo.FullName, targetInfo.CreationTime);
			File.SetLastAccessTime(targetInfo.FullName, targetInfo.LastAccessTime);
			File.SetLastWriteTime(targetInfo.FullName, targetInfo.LastWriteTime);
		}
		public void UpdateDateTimeForDirectoryInfo(DirectoryInfo sourceInfo, DirectoryInfo targetInfo) {
			DateTime?[] defaultDateTimes = [sourceInfo.CreationTime, sourceInfo.LastWriteTime, DateTime.Now];
			targetInfo.CreationTime = FindEarliestValidDateTime(defaultDateTimes);
			targetInfo.LastAccessTime = FindEarliestValidDateTime(defaultDateTimes);
			targetInfo.LastWriteTime = FindEarliestValidDateTime(defaultDateTimes);

			// Because DirectoryInfo is only a cached representation
			Directory.SetCreationTime(targetInfo.FullName, targetInfo.CreationTime);
			Directory.SetLastAccessTime(targetInfo.FullName, targetInfo.LastAccessTime);
			Directory.SetLastWriteTime(targetInfo.FullName, targetInfo.LastWriteTime);
		}
		public void UpdateDateTimeForFileInfo(FileInfo sourceInfo, FileInfo targetInfo) {
			DateTime?[] defaultDateTimes = [sourceInfo.CreationTime, sourceInfo.LastWriteTime, DateTime.Now];
			targetInfo.CreationTime = FindEarliestValidDateTime(defaultDateTimes);
			targetInfo.LastAccessTime = FindEarliestValidDateTime(defaultDateTimes);
			targetInfo.LastWriteTime = FindEarliestValidDateTime(defaultDateTimes);

			// Because FileInfo is only a cached representation
			File.SetCreationTime(targetInfo.FullName, targetInfo.CreationTime);
			File.SetLastAccessTime(targetInfo.FullName, targetInfo.LastAccessTime);
			File.SetLastWriteTime(targetInfo.FullName, targetInfo.LastWriteTime);
		}

		public string ShortenPath(string path, int maxLength, string spacer = "…", char? defaultDirectorySeparator = default, bool throwRangeException = false) {
			if(maxLength <= spacer.Length) {
				if(throwRangeException) {
					throw new ArgumentOutOfRangeException($"Length of {nameof(maxLength)} is ({maxLength}<= than {1 + spacer.Length})");
				} else {
					return spacer.Substring(0, spacer.Length > maxLength ? maxLength : spacer.Length);
				}
			}
			if(string.IsNullOrWhiteSpace(path)) {
				return string.Empty;
			}
			char directorySeparator = defaultDirectorySeparator ?? Path.DirectorySeparatorChar;

			path = path.Replace('\\', '/').Replace('/', directorySeparator);

			if(path.Length <= maxLength) {
				return path;
			}

			List<string> parts = path.Split(directorySeparator).ToList();

			List<string> startParts = new List<string>();
			List<string> endParts = new List<string>();

			string shortenedPath = string.Empty;

			int countPartsAdded = 0;
			int countPartsTotal = parts.Count;
			while(shortenedPath.Length <= maxLength && countPartsAdded < countPartsTotal) {
				if(countPartsAdded % 2 == 0) {
					endParts.Insert(0, parts[countPartsTotal - countPartsAdded - 1]);
				} else {
					startParts.Add(parts[countPartsAdded / 2]);
				}
				countPartsAdded++;

				string tempVal = string.Join(directorySeparator, startParts.Concat(new List<string>() { spacer }).Concat(endParts));
				if(tempVal.Length <= maxLength) {
					shortenedPath = tempVal;
				} else {
					break;
				}
			}
			// Only add last if nothing is added to retVal.
			if(shortenedPath == string.Empty) {
				shortenedPath = parts.Last();
				if(shortenedPath.Length + spacer.Length <= maxLength) {
					shortenedPath = spacer + shortenedPath;
				} else {
					shortenedPath = spacer + shortenedPath.Substring(shortenedPath.Length - (maxLength - spacer.Length));
				}
			}

			// Extra trim from the beginning:
			if(shortenedPath.Length > maxLength) {
				shortenedPath.Substring(shortenedPath.Length - maxLength, maxLength);

			}
			return shortenedPath;
		}


		public static IDictionary<HashCalculator.HashType, string> ComputeHashes(string filePath, IList<HashCalculator.HashType> hashTypes) {
			HashCalculator hashCalculator = new HashCalculator();
			return hashCalculator.ComputeHashes(filePath, hashTypes);
		}
	}
}