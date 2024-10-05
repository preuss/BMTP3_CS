using BMTP3_CS.CompareFiles;
using OnixLabs.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS {
	internal class Program2_TestCompare {
		public static void Main_(string[] args) {
			string firstCompareFolder = @"c:\Backup\Backup\iPhone14\";
			string secondCompareFolder = @"c:\temp\test.backup.iPhone.source\delme\";

			//firstCompareFolder = @"C:\Backup\Backup\iPhone14\";
			//secondCompareFolder = @"C:\temp\test.backup.iPhone.source\delme\";

			string saveToCsvPath = @"C:\temp\test.results.csv";
			firstCompareFolder = @"c:\Private.Testing\test.source\iPhone14\";
			secondCompareFolder = @"c:\Private.Testing\test.target\delme\";

			List<FileInfo> files = TraverseFolder(firstCompareFolder);

			var p = new Program2_TestCompare();

			int limitCompare = 0; // 0 is not limit.

			int[] bufferKBSizes = { 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
			//int[] bufferKBSizes = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288 };
			//int[] bufferKBSizes = { 128, 256, 512, 1024, 2048 };

			List<FileCompareResult> results = new List<FileCompareResult>();

			Console.WriteLine("Start comparing files...");
			Console.WriteLine($"Count files: {files.Count}");

			results.AddRange(p.CompareAllOfFileType<ReadFileInChunksAndCompareSequenceEqual>(firstCompareFolder, secondCompareFolder, files, bufferKBSizes));
			results.AddRange(p.CompareAllOfFileType<ReadFileInChunksAndCompareMemCmp>(firstCompareFolder, secondCompareFolder, files, bufferKBSizes));
			results.AddRange(p.CompareAllOfFileType<ReadFileInChunksAndCompareAvx2>(firstCompareFolder, secondCompareFolder, files, bufferKBSizes));
			results.AddRange(p.CompareAllOfFileType<ReadFileInChunksAndCompareVector>(firstCompareFolder, secondCompareFolder, files, bufferKBSizes));
			results.AddRange(p.CompareAllOfFileType<ReadFileInChunksAndCompareEightByteAtOnce>(firstCompareFolder, secondCompareFolder, files, bufferKBSizes));
			results.AddRange(p.CompareAllOfFileType<ReadFileInChunksAndCompareOneByteAtATime>(firstCompareFolder, secondCompareFolder, files, bufferKBSizes));
			results.AddRange(p.CompareAllOfFileType<ReadFileInChunksAndCompareSequenceEqual>(firstCompareFolder, secondCompareFolder, files, bufferKBSizes));
			results.AddRange(p.CompareAllOfFileType<ReadFileInChunksAndCompareXBytesAtOnce>(firstCompareFolder, secondCompareFolder, files, bufferKBSizes));
			results.AddRange(p.CompareAllOfFileType<ReadWholeFileAtOnceAndCompareUsingLinq>(firstCompareFolder, secondCompareFolder, files, bufferKBSizes));
			results.AddRange(p.CompareAllOfFileType<ReadWholeFileAtOnceAndUseSequenceEquals>(firstCompareFolder, secondCompareFolder, files, bufferKBSizes));
			results.AddRange(p.CompareAllOfFileType<ReadWholeFileAtOnceCompareEightByteAtOnce>(firstCompareFolder, secondCompareFolder, files, bufferKBSizes));

			SaveToCsv(results, saveToCsvPath);
		}

		private List<FileCompareResult> CompareAllOfFileType<T>(string firstCompareFolder, string secondCompareFolder, List<FileInfo> files, int[] bufferSizesKB) where T : FileComparer {
			List<FileCompareResult> results = new List<FileCompareResult>();

			if(typeof(ReadFileInChunks).IsAssignableFrom(typeof(T))) {
				FileCompareResult result;
				foreach(var bufferSizeKB in bufferSizesKB) {
					ReadFileInChunks comparer = (ReadFileInChunks)Activator.CreateInstance(typeof(T), bufferSizeKB * 1024)!;
					result = CompareTwoFiles(firstCompareFolder, secondCompareFolder, files, comparer);
					results.Add(result);
				}
			} else {
				T comparer = (T)Activator.CreateInstance(typeof(T))!;
				FileCompareResult result = CompareTwoFiles(firstCompareFolder, secondCompareFolder, files, comparer);
				results.Add(result);

			}

			return results;
		}


		private FileCompareResult CompareTwoFiles(string sourceFolder, string targetFolder, List<FileInfo> files, FileComparer fileComparer, int limitCompare = 0) {
			return CompareTwoFilesInternal(sourceFolder, targetFolder, files, fileComparer, 0, limitCompare);
		}

		private FileCompareResult CompareTwoFiles(string sourceFolder, string targetFolder, List<FileInfo> files, ReadFileInChunks fileComparer, int limitCompare = 0) {
			int bufferSizeKB = fileComparer.ChunkSize / 1024;
			return CompareTwoFilesInternal(sourceFolder, targetFolder, files, fileComparer, bufferSizeKB, limitCompare);
		}
		private FileCompareResult CompareTwoFilesInternal(string sourceFolder, string targetFolder, List<FileInfo> files, FileComparer fileComparer, int bufferSizeKB, int limitCompare) {
			Console.WriteLine($"Start comparing files using: {fileComparer.GetType().Name}");
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			int countCompared = 0;
			int countTrue = 0;
			double totalFileSizeKB = 0;

			foreach(FileInfo file in files) {
				string relativePath = file.FullName.Replace(sourceFolder, "");
				string targetFilePath = Path.Combine(targetFolder, relativePath);
				FileInfo targetFileInfo = new FileInfo(targetFilePath);
				if(file.Exists && targetFileInfo.Exists) {
					countCompared++;
					totalFileSizeKB += file.Length / 1024.0; // Convert to KB
					if(fileComparer.Compare(file, targetFileInfo)) {
						countTrue++;
					}
				}
				if(limitCompare > 0 && countCompared > limitCompare) {
					break;
				}
			}

			stopwatch.Stop();
			TimeSpan timeTaken = stopwatch.Elapsed;
			double averageTimePerFile = timeTaken.TotalMilliseconds / countCompared;
			double averageFileSize = totalFileSizeKB / countCompared; // Already in KB

			if(bufferSizeKB > 0) {
				Console.WriteLine($"Buffer Size Used:               : {bufferSizeKB} KB");
			}
			Console.WriteLine($"Total Time Taken                : {stopwatch.ElapsedMilliseconds} ms");
			Console.WriteLine($"Total Files Compared            : {countCompared}");
			Console.WriteLine($"Total Time taken                : {timeTaken.Minutes:D2}:{timeTaken.Seconds:D2}.{timeTaken.Milliseconds:D3}");
			Console.WriteLine($"Total Storage Compared          : {totalFileSizeKB / 1024 / 1024:F2} GB");
			Console.WriteLine($"Average Compared File Time Taken: {averageTimePerFile:F5} ms per file");
			Console.WriteLine($"Average Compared File Size      : {averageFileSize:F2} KB");
			if(countCompared != countTrue) {
				Console.WriteLine($"Not all files compared: {countCompared - countTrue}");
			}
			Console.WriteLine();
			Console.WriteLine();

			FileCompareResult result = new FileCompareResult() {
				BufferSizeKB = bufferSizeKB > 0 ? bufferSizeKB : 0,
				TotalTimeTakenMS = stopwatch.ElapsedMilliseconds,
				TotalFilesCompared = countCompared,
				AverageTimePerFileMS = averageTimePerFile,
				AverageFileSizeKB = averageFileSize,
				ComparisonWorksCorrect = (countCompared != countTrue),
				CompareType = fileComparer.GetType().Name
			};
			return result;
		}

		public static List<FileInfo> TraverseFolder(string sourceFolder) {
			List<string> filePaths = new List<string>();

			// All files recursive.
			EnumerationOptions enumerationOptions = new EnumerationOptions() {
				RecurseSubdirectories = true,
				AttributesToSkip = 0,
				MatchType = MatchType.Win32,
				IgnoreInaccessible = false,

				BufferSize = 16 * 1024 // Just a test, it do not need to be set.
			};


			filePaths = Directory.GetFiles(sourceFolder, "*", enumerationOptions).ToList();


			List<FileInfo> files = filePaths
				.Select(x => new FileInfo(x))
				.ToList();

			return files;
		}

		public Boolean CompareTwoFiles(FileInfo fileInfoA, FileInfo fileInfoB) {
			const int BUFFER_SIZE = 8 * 1024;
			//FileComparer fileComparer = new ReadFileInChunksAndCompareAvx2(8 * 1024);
			//FileComparer fileComparer = new Md5Comparer(8 * 1024);
			//FileComparer fileComparer = new ReadFileInChunksAndCompareVector(8 * 1024);
			FileComparer fileComparer = new ReadFileInChunksAndCompareVector(BUFFER_SIZE);

			return fileComparer.Compare(fileInfoA.FullName, fileInfoB.FullName);
		}
		public static void SaveToCsv(List<FileCompareResult> results, string filePath) {
			var csv = new StringBuilder();
			csv.AppendLine("BufferSizeKB;TotalTimeTakenMS;AverageTimePerFileMS;CompareType;ComparisonWorksCorrect;TotalFilesCompared;AverageFileSizeKB");

			foreach(var result in results) {
				csv.AppendLine($"{result.BufferSizeKB};{result.TotalTimeTakenMS};{result.AverageTimePerFileMS};{result.CompareType};{result.ComparisonWorksCorrect};{result.TotalFilesCompared};{result.AverageFileSizeKB}");
			}

			File.WriteAllText(filePath, csv.ToString());
		}
	}

	public class FileCompareResult {
		public int BufferSizeKB { get; set; }
		public long TotalTimeTakenMS { get; set; }
		public int TotalFilesCompared { get; set; }
		public double AverageTimePerFileMS { get; set; }
		public double AverageFileSizeKB { get; set; }
		public bool ComparisonWorksCorrect { get; set; }
		public string CompareType { get; set; } = string.Empty;
	}
}
