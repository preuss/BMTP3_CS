using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NExifTool;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BMTP3_CS.Metadata {
	internal class NExifTool : AbstractMetadataFileInfo {
		private static readonly ExifTool _exifTool = new ExifTool(new ExifToolOptions());
		public NExifTool(string mediaFilePath) : base(mediaFilePath) {
		}
		public NExifTool(FileInfo mediaFileInfo) : base(mediaFileInfo) {
		}
		private Tag? findTagFrom(IEnumerable<Tag> tags, string findTagName) {
			foreach(Tag tag in tags) {
				if(tag.Name.ToLower().Contains(findTagName.ToLower())) {
					return tag;
				}
			}
			foreach(Tag tag in tags) {
				if(tag.Description.ToLower().Contains(findTagName.ToLower())) {
					return tag;
				}
			}
			return null;
		}
		public override DateTime? GetCreatedMediaFileDateTime() {
			IEnumerable<Tag> tags = _exifTool.GetTagsAsync(GetSourceFileInfo().FullName).GetAwaiter().GetResult();
			Console.WriteLine("Tags: ");

			string[] findTagNames = {
				"DateTimeOriginal",
				"MediaCreateDate",
				"Date/Time Digitized",
				"dcterms:created",
			};

			foreach(Tag tag in tags) {
				if(tag.IsDate
				|| tag.Description.Contains("date", StringComparison.InvariantCultureIgnoreCase)
				|| tag.Description.Contains("time", StringComparison.InvariantCultureIgnoreCase)
				|| tag.Name.Contains("date", StringComparison.InvariantCultureIgnoreCase)
				|| tag.Name.Contains("time", StringComparison.InvariantCultureIgnoreCase)
				) {
					Console.WriteLine("\tA Tag:");
					Console.WriteLine($"\t\tID: {tag.Id}");
					Console.WriteLine($"\t\tTableName: {tag.TableName}");
					Console.WriteLine($"\t\tName: {tag.Name}");
					Console.WriteLine($"\t\tValue: {tag.Value}");
					Console.WriteLine($"\t\tDescription: {tag.Description}");
				}
			}

			Tag? found = null;
			foreach(var findTagName in findTagNames) {
				found = findTagFrom(tags, findTagName);
				if(found != null) {
					break;
				}
			}
			if(found != null) {
				if(found.IsDate) {
					return found.GetDateTime();
				} else {
					return null;
				}
			}
			return null;

			/*
			foreach(Tag tag in tags) {
				if(tag.IsDate
				|| tag.Description.Contains("date", StringComparison.InvariantCultureIgnoreCase)
				|| tag.Description.Contains("time", StringComparison.InvariantCultureIgnoreCase)
				|| tag.Name.Contains("date", StringComparison.InvariantCultureIgnoreCase)
				|| tag.Name.Contains("time", StringComparison.InvariantCultureIgnoreCase)
				) {
					Console.WriteLine("\tA Tag:");
					Console.WriteLine($"\t\tID: {tag.Id}");
					Console.WriteLine($"\t\tTableName: {tag.TableName}");
					Console.WriteLine($"\t\tName: {tag.Name}");
					Console.WriteLine($"\t\tValue: {tag.Value}");
					Console.WriteLine($"\t\tDescription: {tag.Description}");
				}
			}
			return null;
			*/
		}
	}
}
