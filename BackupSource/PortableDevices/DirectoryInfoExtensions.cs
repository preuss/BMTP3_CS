using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BMTP3_CS.BackupSource.PortableDevices {
	/// <summary>
	/// Provides extension methods for the DirectoryInfo class.
	/// </summary>
	public static class DirectoryInfoExtensions {
		/// <summary>
		/// Creates a temporary directory with a specified prefix in the directory represented by this DirectoryInfo instance.
		/// </summary>
		/// <param name="directoryInfo">The DirectoryInfo instance on which this method is invoked.</param>
		/// <param name="prefix">The prefix for the temporary directory.</param>
		/// <returns>The full path of the created temporary directory.</returns>
		/// <remarks>
		/// This method uses the TempDirectory.CreateTempDirectory method internally to create the temporary directory.
		/// The prefix parameter is used to create a unique name for the temporary directory.
		/// </remarks>
		public static DirectoryInfo CreateTempDirectory(this DirectoryInfo directoryInfo, string prefix) {
			return new DirectoryInfo(TempDirectory.CreateTempDirectory(prefix, directoryInfo.FullName));
		}

		public static FileInfo CombineToFileInfo(this DirectoryInfo directoryInfo, string fileName) {
			return new FileInfo(Path.Combine(directoryInfo.FullName, fileName));
		}
	}
}
