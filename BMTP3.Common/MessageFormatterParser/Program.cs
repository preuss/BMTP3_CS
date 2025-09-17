using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser {
	// Example usage
	public class Program {
		public static void Main() {
			var template = "Backup: ${filename.toUpper()} on ${date:yyyy,MM/dd:EEE ${filename}}";
			var values = new Dictionary<string, object>
			{
			{ "filename", "photo.jpg" },
			{ "date", "2025-04-17" }
		};

			try {
				var result = new MessageFormatter().Format(template, values);
				Console.WriteLine(result); // Expected: Backup: PHOTO.JPG on 2025,04/17:Thu photo.jpg
			} catch(Exception ex) {
				Console.WriteLine($"Error: {ex.Message}");
			}

			// Example with condition
			template = "Files: ${count,if,eq0?no files:${other} files}";
			values = new Dictionary<string, object>
			{
			{ "count", 2.0 },
			{ "other", "two" }
		};

			try {
				var result = new MessageFormatter().Format(template, values);
				Console.WriteLine(result); // Expected: Files: two files
			} catch(Exception ex) {
				Console.WriteLine($"Error: {ex.Message}");
			}
		}
	}
}
