using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.BackupSource.PortableDevices {
	internal class TempDirectory {
		[DllImport(@"kernel32.dll", EntryPoint = "CreateDirectory", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CreateDirectoryApi([MarshalAs(UnmanagedType.LPTStr)] string lpPathName, nint lpSecurityAttributes);
		/// <summary>
		/// Creates the directory if it does not exist.
		/// </summary>
		/// <param name="directoryPath">The directory path.</param>
		/// <returns>Returns false if directory already exists. Exceptions for any other errors</returns>
		/// <exception cref="Win32Exception"></exception>
		internal static bool CreateDirectoryIfItDoesNotExist([NotNull] string directoryPath) {
			if(directoryPath == null) throw new ArgumentNullException("directoryPath");

			// First ensure parent exists, since the WIN Api does not
			CreateParentFolder(directoryPath);

			if(!CreateDirectoryApi(directoryPath, lpSecurityAttributes: nint.Zero)) {
				Win32Exception lastException = new Win32Exception();

				const int ERROR_ALREADY_EXISTS = 183;
				if(lastException.NativeErrorCode == ERROR_ALREADY_EXISTS) return false;

				throw new IOException(
					"An exception occurred while creating directory'" + directoryPath + "'" + Environment.NewLine + lastException);
			}

			return true;
		}
		internal static DirectoryInfo? CreateParentFolder(string directoryPath) {
			DirectoryInfo? parentInfo = Directory.GetParent(directoryPath);

			if(parentInfo == null) {
				throw new Exception($"The parent does not exist of 'directoryPath'");
			}
			if(!parentInfo.Exists) {
				parentInfo.Create();
			}

			return parentInfo;
		}
		private static readonly object LockObject = new();
		/// <summary>
		/// Opretter et midlertidigt bibliotek med et unikt navn i den angivne sti.
		/// </summary>
		/// <param name="prefix">Præfikset til det midlertidige bibliotek.</param>
		/// <param name="dirPath">Stien, hvor det midlertidige bibliotek skal oprettes.</param>
		/// <returns>Returnerer stien til det oprettede midlertidige bibliotek.</returns>
		/// <remarks>
		/// Denne metode genererer et unikt navn for det midlertidige bibliotek ved at kombinere præfikset med et tilfældigt filnavn.
		/// Hvis et bibliotek med det genererede navn allerede findes, genereres et nyt navn, indtil et unikt navn er fundet.
		/// Navngenerering og biblioteksoprettelse er trådsikre for at undgå racebetingelser.
		/// </remarks>
		public static string CreateTempDirectory(string prefix, string dirPath) {
			string tmpDirectory;
			lock(LockObject) {
				do {
					tmpDirectory = Path.Combine(dirPath, prefix + Path.GetRandomFileName());
				} while(!CreateDirectoryIfItDoesNotExist(tmpDirectory));
			}
			return tmpDirectory;
		}
	}
}
