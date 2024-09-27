using BMTP3_CS.FilecCmparer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS {
	internal class Program2_TestCompare {
		public static void Main(string[] args) {
			const string sourceFolder = "c:\\Backup\\Backup\\iPhone14\\";
			const string targetFolder = "c:\\temp\\test.backup.iPhone.source\\delme\\";

			List<FileInfo> files = TraverseFolder(sourceFolder);

			var p = new Program2_TestCompare();

			int limitCompare = 0; // 0 is not limit.

			Console.WriteLine("Start comparing files...");
			Console.WriteLine($"Count files: {files.Count}");

			Console.WriteLine("Start comparing files using: ReadFileInChunksAndCompareVector");
			p.CompareTwoFiles(sourceFolder, targetFolder, files, (source, target, bufferSize) => new ReadFileInChunksAndCompareVector(source, target, bufferSize), limitCompare);

			Console.WriteLine("Start comparing files using: ReadFileInChunksAndCompareVector");
			p.CompareTwoFiles(sourceFolder, targetFolder, files, (source, target, bufferSize) => new ReadFileInChunksAndCompareVector(source, target, bufferSize), limitCompare);

			Console.WriteLine("Start comparing files using: ReadFileInChunksAndCompareAvx2");
			p.CompareTwoFiles(sourceFolder, targetFolder, files, (source, target, bufferSize) => new ReadFileInChunksAndCompareAvx2(source, target, bufferSize), limitCompare);

			Console.WriteLine("Start comparing files using: ReadWholeFileAtOnceCompareEightByteAtOnce");
			p.CompareTwoFiles(sourceFolder, targetFolder, files, (source, target, bufferSize) => new ReadWholeFileAtOnceCompareEightByteAtOnce(source, target), limitCompare);

			Console.WriteLine("Start comparing files using: ReadWholeFileAtOnce");
			p.CompareTwoFiles(sourceFolder, targetFolder, files, (source, target, bufferSize) => new ReadWholeFileAtOnce(source, target), limitCompare);

			Console.WriteLine("Start comparing files using: Md5Comparer");
			p.CompareTwoFiles(sourceFolder, targetFolder, files, (source, target, bufferSize) => new Md5Comparer(source, target), limitCompare);

			Console.WriteLine("Start comparing files using: ReadWholeFileAtOnceAndCompareUsingLinq");
			p.CompareTwoFiles(sourceFolder, targetFolder, files, (source, target, bufferSize) => new ReadWholeFileAtOnceAndCompareUsingLinq(source, target), limitCompare);
		}
		private void CompareTwoFiles(string sourceFolder, string targetFolder, List<FileInfo> files, Func<string, string, int, FileComparer> fileComparerFactory, int limitCompare = 0) {
			const int BUFFER_SIZE = 8 * 1024;

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
					FileComparer fileComparer = fileComparerFactory(file.FullName, targetFileInfo.FullName, BUFFER_SIZE);
					if(fileComparer.Compare()) {
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
			//FileComparer fileComparer = new ReadFileInChunksAndCompareAvx2(targetTempFileInfo.FullName, newTargetFilePath, 8 * 1024);
			//FileComparer fileComparer = new Md5Comparer(targetTempFileInfo.FullName, newTargetFilePath, 8 * 1024);
			//FileComparer fileComparer = new ReadFileInChunksAndCompareVector(targetTempFileInfo.FullName, newTargetFilePath, 8 * 1024);
			FileComparer fileComparer = new ReadFileInChunksAndCompareVector(fileInfoA.FullName, fileInfoB.FullName, BUFFER_SIZE);

			return fileComparer.Compare();
		}
	}
}
