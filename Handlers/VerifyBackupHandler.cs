using IniParser;
using IniParser.Model;
using OnixLabs.Core.Linq;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BMTP3_CS.Handlers.HashCalculator;

namespace BMTP3_CS.Handlers {
	public class VerifyBackupHandler {
		private IAnsiConsole Console { get; init; }
		private PrintHandler PrinterHandler { get; init; }
		private HashCalculator HashCalculator { get; init; }
		public VerifyBackupHandler(IAnsiConsole console, PrintHandler printHandler, HashCalculator hashCalculator) {
			Console = console;
			PrinterHandler = printHandler;
			HashCalculator = hashCalculator;
		}
		public void VerifyBackup(string backupPath) {
			Console.WriteLine($"Starting VerifyBackup using backupPath: {backupPath}");

			DirectoryInfo backupDirInfo = new DirectoryInfo(backupPath);
			if(!backupDirInfo.Exists) {
				Console.WriteLine($"BackupPath does not exist: {backupPath}");

			} else {
				IList<FileInfo> files = ReadAllFilesRecursive(backupDirInfo);

				IList<FileInfo> fileSidecars = files.Where(x => x.Name.EndsWith(".ini")).ToList();

				Console.WriteLine($"Files Count {files.Count}");
				Console.WriteLine($"SideCars Count {fileSidecars.Count}");

				int countFiles = 0;
				int countFilesTotal = fileSidecars.Count;
				foreach(FileInfo sideCar in fileSidecars) {
					countFiles++;
					if(countFiles % 100 == 0) {
						Console.WriteLine($"Now as Count {countFiles} of {countFilesTotal}");
					}
					var parser = new FileIniDataParser();
					IniData data = parser.ReadFile(sideCar.FullName);

					var sha3_512_hash_str = data["FileHash"]["SHA3_512"] ?? string.Empty;
					var md5_128_str = data["FileHash"]["MD5"] ?? string.Empty;
					var blake3_256_hash_str = data["FileHash"]["BLAKE3"] ?? string.Empty;

					var originalFileName = data["Settings"]["OriginalFileName"] ?? string.Empty;

					FileInfo currentFile = new FileInfo(Path.Combine(sideCar.DirectoryName!, originalFileName));
					IDictionary<HashType, string> hashes = HashCalculator.ComputeHashes(currentFile.FullName, [/*HashType.SHA3_512_FIPS202, HashType.SHA3_512_KECCAK, */HashType.SHA2_512, HashType.MD5_128, HashType.BLAKE3_256]);

					if(hashes[HashType.SHA2_512] != sha3_512_hash_str) {
						Console.WriteLine("Hash HashType.SHA3_512_FIPS202 not the same");
						//						Console.WriteLine($"HashType.SHA3_512_FIPS202: {hashes[HashType.SHA3_512_FIPS202]}");
						//						Console.WriteLine($"HashType.SHA3_512_KECCAK : {hashes[HashType.SHA3_512_KECCAK]}");
						Console.WriteLine($"SHA3_512                 : {sha3_512_hash_str}");
					}
					if(hashes[HashType.MD5_128] != md5_128_str) {
						Console.WriteLine("Hash HashType.MD5_128 not the same");
					}
					if(hashes[HashType.BLAKE3_256] != blake3_256_hash_str) {
						Console.WriteLine("Hash HashType.BLAKE3_256 not the same");
					}

				}
			}
		}

		public IList<FileInfo> ReadAllFilesRecursive(DirectoryInfo readDirectory) {
			List<FileInfo> files = new List<FileInfo>();

			List<DirectoryInfo> dirsToRead = new List<DirectoryInfo>();
			dirsToRead.Add(readDirectory);
			while(dirsToRead.IsNotEmpty()) {
				List<DirectoryInfo> newDirsToRead = new List<DirectoryInfo>();
				foreach(DirectoryInfo dir in dirsToRead) {
					newDirsToRead.AddRange(dir.GetDirectories());
					files.AddRange(dir.GetFiles());
				}
				dirsToRead = newDirsToRead;
			}

			return files;
		}
	}
}
