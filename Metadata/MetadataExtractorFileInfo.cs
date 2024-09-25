using MetadataExtractor.Formats.Exif;
using MetadataExtractor;
using Directory = MetadataExtractor.Directory;
using MetadataExtractor.Formats.QuickTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Metadata {
	internal class MetadataExtractorFileInfo : AbstractMetadataFileInfo {
		public static readonly DateTime EPOCH_DATETIME = new DateTime(1904, 1, 1);
		public MetadataExtractorFileInfo(string mediaFilePath) : base(mediaFilePath) {
		}
		public MetadataExtractorFileInfo(FileInfo mediaFileInfo) : base(mediaFileInfo) {
		}
		public override DateTime? GetCreatedMediaFileDateTime() {
			IEnumerable<MetadataExtractor.Directory> directories;
			try {
				directories = MetadataExtractor.ImageMetadataReader.ReadMetadata((GetSourceFileInfo().FullName));
			} catch(ImageProcessingException e) {
				return null;
			}

			DateTime? dateTime = null;
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(MetadataExtractor.Formats.Exif.ExifSubIfdDirectory), ExifDirectoryBase.TagDateTimeDigitized);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(MetadataExtractor.Formats.Exif.ExifSubIfdDirectory), ExifDirectoryBase.TagDateTimeOriginal);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(MetadataExtractor.Formats.Exif.ExifDirectoryBase), ExifDirectoryBase.TagDateTimeDigitized);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(MetadataExtractor.Formats.Exif.ExifDirectoryBase), ExifDirectoryBase.TagDateTimeOriginal);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(MetadataExtractor.Formats.Xmp.XmpDirectory), ExifDirectoryBase.TagDateTimeDigitized);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(MetadataExtractor.Formats.Xmp.XmpDirectory), ExifDirectoryBase.TagDateTimeOriginal);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(MetadataExtractor.Formats.QuickTime.QuickTimeMetadataHeaderDirectory), QuickTimeMetadataHeaderDirectory.TagCreationDate);
			}
			if(dateTime == null) {
				dateTime = GetDateTime(directories, typeof(MetadataExtractor.Formats.QuickTime.QuickTimeMovieHeaderDirectory), QuickTimeMovieHeaderDirectory.TagCreated);
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