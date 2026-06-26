using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using Directory = MetadataExtractor.Directory;

namespace BMTP3.Core.Metadata {
	internal class MetadataExtractorFileInfo : AbstractMetadataFileInfo {
		public static readonly DateTime EPOCH_DATETIME = new DateTime(1904, 1, 1);
		public MetadataExtractorFileInfo(string mediaFilePath) : base(mediaFilePath) {
		}
		public MetadataExtractorFileInfo(FileInfo mediaFileInfo) : base(mediaFileInfo) {
		}
		public override DateTime? GetCreatedMediaFileDateTime() {
			IEnumerable<Directory> directories;
			try {
				directories = ImageMetadataReader.ReadMetadata(GetSourceFileInfo().FullName);
			} catch(ImageProcessingException) {
				//TODO: Try to cleanup this code, we should not read metadata if not an image.
				return null;
			} catch(ArgumentOutOfRangeException) {
				// Problem with ImageMetadataReader, it has some serious problems.
				// TODO: Switch to a bette metadata reader
				return null;
			}

			DateTime? dateTime = null;
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(ExifSubIfdDirectory), ExifDirectoryBase.TagDateTimeDigitized);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(ExifSubIfdDirectory), ExifDirectoryBase.TagDateTimeOriginal);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(ExifDirectoryBase), ExifDirectoryBase.TagDateTimeDigitized);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(ExifDirectoryBase), ExifDirectoryBase.TagDateTimeOriginal);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(MetadataExtractor.Formats.Xmp.XmpDirectory), ExifDirectoryBase.TagDateTimeDigitized);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(MetadataExtractor.Formats.Xmp.XmpDirectory), ExifDirectoryBase.TagDateTimeOriginal);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(QuickTimeMetadataHeaderDirectory), QuickTimeMetadataHeaderDirectory.TagCreationDate);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(QuickTimeMovieHeaderDirectory), QuickTimeMovieHeaderDirectory.TagCreated);
			}
			return dateTime;
		}
		private DateTime? GetDateTime(IEnumerable<Directory> directories, Type directoryType, int tagType) {
			foreach(Directory directory in directories) {
				if(directoryType.IsInstanceOfType(directory)) {
					try {

						if(directory.TryGetDateTime(tagType, out var dateTime)) {
							// This MetadataExtractor returns EPOCH if QuickTime Date is empty / null.
							if(EPOCH_DATETIME.Equals(dateTime)) {
								return null;
							}
							return dateTime;
						}
					} catch(Exception ex) {
						Console.WriteLine($"Error extracting DateTime: {ex.Message}");
						throw new Exception($"Error extracting DateTime: {ex.Message}");
					}
				}
			}
			return null;
		}
	}
}