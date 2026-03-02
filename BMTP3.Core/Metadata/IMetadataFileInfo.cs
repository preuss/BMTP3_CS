namespace BMTP3.Core.Metadata {
	internal interface IMetadataFileInfo {
		public DateTime? GetCreatedMediaFileDateTime();
		public FileInfo GetSourceFileInfo();
	}
}
