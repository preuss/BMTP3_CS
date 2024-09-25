using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Metadata {
	internal abstract class AbstractMetadataFileInfo : IMetadataFileInfo {
		private readonly FileInfo mediaFileInfo;
		public AbstractMetadataFileInfo(string mediaFilePath) {
			this.mediaFileInfo = new FileInfo(mediaFilePath);
		}
		public AbstractMetadataFileInfo(FileInfo mediaFileInfo) {
			this.mediaFileInfo = mediaFileInfo;
		}
		public abstract DateTime? GetCreatedMediaFileDateTime();
		public FileInfo GetSourceFileInfo() {
			return mediaFileInfo;
		}
		public static IMetadataFileInfo FromFileInfo(FileInfo mediaFileInfo) {
			return new MetadataExtractorFileInfo(mediaFileInfo);
		}
	}
}
