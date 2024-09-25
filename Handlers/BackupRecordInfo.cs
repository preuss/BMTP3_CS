using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Handlers {
	internal class BackupRecordInfo : IEquatable<BackupRecordInfo> {
		public static readonly DateTime MinWin32FileTime = DateTime.FromFileTimeUtc(0);
		public string PersistentUniqueId { get; set; }
		public string Path { get; set; }
		public string? Name { get; set; }
		public ulong Size { get; set; }
		public DateTime? DateCreated { get; set; }
		public DateTime? DateModified { get; set; }
		public DateTime? DateAuthored { get; set; }
		public bool IsSaved { get; set; }
		/// <summary>
		/// This is generated, because PersistentUniqueId is not really Persistent
		/// </summary>
		[JsonProperty]
		public string? GeneratedId { get; private set; }

		public BackupRecordInfo(string persistentUniqueId, string path, ulong size) {
			if(string.IsNullOrWhiteSpace(persistentUniqueId)) {
				throw new ArgumentException("PersistentUniqueId cannot be empty or whitespace.", nameof(persistentUniqueId));
			}
			if(string.IsNullOrWhiteSpace(path)) {
				throw new ArgumentException("Path cannot be empty or whitespace.", nameof(path));
			}
			PersistentUniqueId = persistentUniqueId;
			Path = path;
			Size = size;
		}

		public void UpdateGeneratedId() {
			//GeneratedId = $"{Name}_{Path}_{Size}_{DateCreated?.Ticks}";

			// Remove fileName from Path
			string folderPath = System.IO.Path.GetDirectoryName(Path) ?? "";
			GeneratedId = $"{folderPath}_{Size}_{DateCreated?.Ticks}_{DateModified?.Ticks}";
		}

		public override bool Equals(object? otherObj) {
			return Equals(otherObj as BackupRecordInfo);
		}

		public bool Equals(BackupRecordInfo? other) {
			if(other == null)
				return false;

			// Compare relevant fields for equality
			return PersistentUniqueId == other.PersistentUniqueId
				&& Path == other.Path
				&& Size == other.Size
				&& DateCreated == other.DateCreated
				&& DateModified == other.DateModified
				&& DateAuthored == other.DateAuthored;
		}

		public override int GetHashCode() {
			// Implement a suitable hash code generation based on relevant fields
			return HashCode.Combine(PersistentUniqueId, Path, Size, DateCreated, DateModified, DateAuthored);
		}
	}
}
