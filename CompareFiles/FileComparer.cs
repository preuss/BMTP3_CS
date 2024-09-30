using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.CompareFiles {
	public abstract class FileComparer {
		/// <summary>
		/// Base class for creating a file comparer
		/// </summary>
		protected FileComparer() {
		}

		/// <summary>
		/// Compares the two given files and returns true if the files are the same
		/// </summary>
		/// <returns>true if the files are the same, false otherwise</returns>
		public bool Compare(string filePath1, string filePath2) {
			FileInfo fileInfo1 = new FileInfo(filePath1);
			FileInfo fileInfo2 = new FileInfo(filePath2);
			return Compare(fileInfo1, fileInfo2);
		}
		public bool Compare(FileInfo fileInfo1, FileInfo fileInfo2) {
			EnsureFilesExist(fileInfo1, fileInfo2);
			if(IsDifferentLength(fileInfo1, fileInfo2)) {
				return false;
			}
			if(IsSameFile(fileInfo1, fileInfo2)) {
				return true;
			}
			return OnCompare(fileInfo1, fileInfo2);
		}

		/// <summary>
		/// Compares the two given files and returns true if the files are the same
		/// </summary>
		/// <returns>true if the files are the same, false otherwise</returns>
		protected abstract bool OnCompare(FileInfo FileInfo1, FileInfo FileInfo2);

		private bool IsSameFile(FileInfo FileInfo1, FileInfo FileInfo2) {
			return string.Equals(FileInfo1.FullName, FileInfo2.FullName, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Does an early comparison by checking files Length, if lengths are not the same, files are definetely different
		/// </summary>
		/// <returns>true if different length</returns>
		private bool IsDifferentLength(FileInfo FileInfo1, FileInfo FileInfo2) {
			return FileInfo1.Length != FileInfo2.Length;
		}

		/// <summary>
		/// Makes sure files exist
		/// </summary>
		private void EnsureFilesExist(FileInfo FileInfo1, FileInfo FileInfo2) {
			if(FileInfo1.Exists == false) {
				throw new ArgumentNullException(nameof(FileInfo1));
			}
			if(FileInfo2.Exists == false) {
				throw new ArgumentNullException(nameof(FileInfo2));
			}
		}

	}
}
