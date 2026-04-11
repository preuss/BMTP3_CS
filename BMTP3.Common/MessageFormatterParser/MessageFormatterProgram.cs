namespace BMTP3.Common.MessageFormatterParser;

// Example usage
public class MessageFormatterProgram
{
	public static void Main()
	{
		string template = "Backup: ${filename.toUpper()} on ${date:yyyy,MM/dd:EEE ${filename}}";
		Dictionary<string, object> values = new()
		{
			{ "filename", "photo.jpg" },
			{ "date", "2025-04-17" }
		};

		try
		{
			string result = new MessageFormatter().Format(template, values);
			Console.WriteLine(result); // Expected: Backup: PHOTO.JPG on 2025,04/17:Thu photo.jpg
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error: {ex.Message}");
		}

		// Example with condition
		template = "Files: ${count,if,eq0?no files:${other} files}";
		values = new Dictionary<string, object>
		{
			{ "count", 2.0 },
			{ "other", "two" }
		};

		try
		{
			string result = new MessageFormatter().Format(template, values);
			Console.WriteLine(result); // Expected: Files: two files
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error: {ex.Message}");
		}
	}
}